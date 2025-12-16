using DungeonCrawler.src.Interpreter;
using Serilog;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

public class ClientStateController : IStateController
{
    // IStateController implementation
    public List<Player> players { get; set; } = [];
    public List<Enemy> enemies { get; set; } = [];
    public WorldGrid worldGrid { get; set; } = new(1337);

    private readonly TcpClient _client; // Don't interact with directly.
    private readonly NetworkStream _stream; // Don't interact with directly. Use ICommand.ExecuteOnClient().
    public ConcurrentQueue<ConsoleKey> InputQueue { get; private set; } = [];

    private LogView _clientLogView = new(); // Helper class to filter relevant messages
    public IReadOnlyList<string> MessagesToDisplay { get; private set; } = []; // List of relevant messages

    public Player? Identity; // Reference to actual local player..
    private readonly object _stateLock = new();
    public IClientState _state;
    public IClientMediator Mediator { get; }

    private ClientStateController(TcpClient client)
    {
        _client = client;
        _stream = client.GetStream();

        Mediator = new ClientMediator(this);

        _state = new ConnectingState();
        _state.Enter(this);
    }

    public static async Task<ClientStateController> CreateAsync(string ipAddress, int port)
    {
        var client = new TcpClient();
        await client.ConnectAsync(ipAddress, port);
        return new ClientStateController(client);
    }
    public void SetState(IClientState state)
    {
        lock (_stateLock)
        {
            _state.Exit(this);
            _state = state;
            _state.Enter(this);
        }
    }
    public Task HandleInput(ConsoleKey key)
    {
        return _state.HandleInput(this, key);
    }

    public async Task Run()
    {
        Log.Debug("Hello, run");
        CancellationTokenSource masterCts = new();
        CancellationToken token = masterCts.Token;

        var listenTask = ListenForServer(token); // Listen for server stream and apply replication ICommands.
        Log.Debug("Created listen task");
        var gameLoopTask = GameLoop(token); // Process queued input. Redundant and replaceable with just a single UI thread.
        Log.Debug("Created gameloop task.");
        var renderLoopTask = RenderLoop(token); // Draw current state.
        Log.Debug("Created render loop task. All tasks created");


        try
        {
            await Task.WhenAll(listenTask, gameLoopTask, renderLoopTask); // None of these must block.
        }
        catch (Exception e)
        {
            Log.Error("Game loop encountered critical error: {e}", e);
            if (!masterCts.IsCancellationRequested)
            {
                masterCts.Cancel();
            }
            throw;
        }
        finally
        {
            Log.Error("All client tasks are done, goodbye");
            _client.Close();
        }
    }

    private async Task RenderLoop(CancellationToken token)
    {
        try
        {
            Log.Debug("Enter render loop ...");
            while (!token.IsCancellationRequested)
            {
                Log.Debug("render event");
                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    await Mediator.NotifyInput(key.Key);
                }

                try
                {
                    Mediator.RequestRender();
                }
                catch (Exception e)
                {
                    Log.Error(e, "render fail");
                    throw;
                }
                await Task.Delay(30, token);
            }

        }
        catch (Exception e)
        {
            Log.Error(e, "WHAT?!!??!");
            throw;
        }
        finally
        {
            Log.Error("you shouldnt be here");
        }
    }

    private async Task GameLoop(CancellationToken token)
    {
        var interpreter = new DungeonCrawler.src.Interpreter.CommandInterpreter();

        while (!token.IsCancellationRequested)
        {
            if (Identity is null && _state is not SyncingState)
            {
                Log.Debug("GameLoop is waiting for synchronization ...");
                await Task.Delay(100, token);
                continue;
            }

            if (InputQueue.TryDequeue(out ConsoleKey key))
            {
                Log.Debug($"Key pressed: {key}");


                
                switch (key)
                {
                    case ConsoleKey.E:
                        var enemyCountVisitor = new EnemyCountVisitor();
                        worldGrid.Accept(enemyCountVisitor);
                        MessageLog.Instance.Add(
                            new LogEntry(Loggers.Game, enemyCountVisitor.GetReport())
                        );
                        continue;

                    case ConsoleKey.I:
                        if (Identity.Room is IVisitableRoom interactableRoom)
                        {
                            var roomInteractionVisitor = new RoomInteractionVisitor(Identity);
                            interactableRoom.Accept(roomInteractionVisitor);
                        }
                        continue;
                }

                // INTERPRETER dalis (judejimas + attack)
                string? input = key switch
                {
                    ConsoleKey.W => "w",
                    ConsoleKey.A => "a",
                    ConsoleKey.S => "s",
                    ConsoleKey.D => "d",
                    ConsoleKey.Spacebar => "space",
                    _ => null
                };

                if (input is null)
                    continue;

                try
                {
                    var expression = interpreter.Parse(input);

                    var context = new GameContext(Identity, worldGrid);

                    ICommand command = expression.Interpret(context);

                    await command.ExecuteOnClient(this);
                }
                catch (InvalidOperationException)
                {
                    Log.Debug("Unknown command input: {input}", input);
                }

                await Mediator.NotifyInput(key);
            }
            await _state.Update(this);
            await Task.Delay(10, token);
        }
    }

    public SyncCommand currentSync;
    private async Task ListenForServer(CancellationToken token)
    {
        try
        {
            byte[] lengthBuffer = new byte[4]; // length prefix, TCP stream counteraction...
            Log.Debug("Hello, listen ...");

            while (!token.IsCancellationRequested)
            {
                await _stream.ReadExactlyAsync(lengthBuffer, 0, 4, token);

                int messageLength = BitConverter.ToInt32(lengthBuffer, 0);
                Log.Debug("Incoming message length {len}", messageLength);

                byte[] messageBuffer = new byte[messageLength];

                await _stream.ReadExactlyAsync(messageBuffer, 0, messageLength, token);
                Log.Debug("first 4 bytes: {b}", messageBuffer[..4]);
                try
                {
                    string jsonWrapperString = Encoding.UTF8.GetString(messageBuffer, 0, messageLength);
                    jsonWrapperString = jsonWrapperString.TrimStart('\uFEFF');

                    Log.Debug("Received server payload {payload}", jsonWrapperString);
                    ICommand? command = JsonSerializer.Deserialize<ICommand>(jsonWrapperString, NetworkSerializer.Options);
                    if (command is null)
                    {
                        Console.WriteLine("Malformed command from server?");
                        continue;
                    }

                    Log.Information($"Executing command from server of type {command.GetType()}");
                    await Mediator.NotifyServerCommand(command);

                }
                catch (Exception e)
                {
                    Log.Error("Unable to deserialize/interpret server command: {e}", e);
                    throw;
                }
            }

        }
        catch (Exception e)
        {
            Log.Error("listen die with {e}", e);
        }
    }

    public void SetIdentity(Guid identity)
    {
        Identity = players.Find((player) => player.Identity.Equals(identity));
        if (Identity is null)
        {
            throw new Exception($"bad server replication. could not find identity {identity}");
        }
        Log.Debug($"Identity updated to {identity}");
    }

    public Character? FindCharacterByIdentity(Guid identity)
    {
        return players.Cast<Character>().Concat(enemies).FirstOrDefault(c => c.Identity == identity);
    }

    /// <summary>
    /// Applies a dumb (entire game state) snapshot to the current world.
    /// </summary>
    /// <param name="snapshot">snapshot</param>
    private readonly Minimap _minimap = new();
    public MinimapTile[,] MinimapDisplay { get; private set; } = new MinimapTile[0, 0];

    public void ApplySnapshot(GameStateSnapshot snapshot)
    {
        Log.Debug("Received snapshot with n={n} players...", snapshot.Players.Count);
        players = snapshot.Players;
        enemies = snapshot.Enemies;
        worldGrid = snapshot.WorldGrid;

        if (Identity != null && Identity.Room != null)
        {
            MinimapDisplay = _minimap.Render(worldGrid, Identity.Room.WorldGridPosition);
        }

        foreach (var room in worldGrid.GetAllRooms())
        {
            room.Occupants.Clear();
        }

        var allCharacters = players.Cast<Character>().Concat(enemies);
        // fix references... todo: use Dtos and proper guid referencing
        foreach (var character in allCharacters)
        {
            var worldPos = character.Room.WorldGridPosition;

            if (worldGrid.Rooms.TryGetValue(worldPos, out Room? canonicalRoom))
            {
                character.Room = canonicalRoom;
                canonicalRoom.Occupants.Add(character);
            }
            else
            {
                Log.Warning("Character {id} had a room at {pos} which was not found in the WorldGrid.", character.Identity, worldPos);
            }
        }

        MessagesToDisplay = _clientLogView.GetRelevantMessages(snapshot.LogEntries);
        Log.Information("got relevant messages ...", MessagesToDisplay);
    }

    public async Task SendCommand(ICommand command)
    {
        try
        {
            byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(command, NetworkSerializer.Options);

            int messageLength = jsonBytes.Length; // required for length-prefixing. TCP can be annoying.
            byte[] lengthPrefix = BitConverter.GetBytes(messageLength);

            try
            {
                await _stream.WriteAsync(lengthPrefix);
                await _stream.WriteAsync(jsonBytes);
                await _stream.FlushAsync();
            }
            catch (Exception e)
            {
                Log.Error($"Failed to replicate to server. Is it still alive?\n{e}");
                throw;
            }


            Log.Debug($"Sent of type {command.GetType()}");

        }
        catch (Exception e)
        {
            Log.Error(e, "terrible...");
        }
    }

}
