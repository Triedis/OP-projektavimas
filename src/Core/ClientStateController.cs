using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Serilog;

class ClientStateController : IStateController
{
    private readonly TcpClient _client; // Don't interact with directly.
    private readonly NetworkStream _stream; // Don't interact with directly. Use ICommand.ExecuteOnClient().
    public ConcurrentQueue<ConsoleKey> InputQueue { get; } = [];

    public Player? Identity; // Reference to actual local player..

    private ClientStateController(TcpClient client)
    {
        _client = client;
        _stream = client.GetStream();
    }

    public static async Task<ClientStateController> CreateAsync(string ipAddress, int port)
    {
        var client = new TcpClient();
        await client.ConnectAsync(ipAddress, port);
        return new ClientStateController(client);
    }

    public override async Task Run()
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
                    InputQueue.Enqueue(key.Key);
                }

                try
                {
                    TerminalRenderer.Render(this);
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
        while (!token.IsCancellationRequested)
        {
            if (Identity is null)
            {
                Log.Debug("GameLoop is waiting for synchronization ...");
                await Task.Delay(100, token);
                continue;
            }

            if (InputQueue.TryDequeue(out ConsoleKey key))
            {
                Log.Debug($"Key pressed: {key}");

                Vector2? moveDirection = null;
                bool shouldUseWeapon = false;
                switch (key)
                {
                    case ConsoleKey.W:
                        moveDirection = new Vector2(0, -1);
                        break;
                    case ConsoleKey.S:
                        moveDirection = new Vector2(0, 1);
                        break;
                    case ConsoleKey.A:
                        moveDirection = new Vector2(-1, 0);
                        break;
                    case ConsoleKey.D:
                        moveDirection = new Vector2(1, 0);
                        break;
                    case ConsoleKey.Spacebar:
                        shouldUseWeapon = true;
                        break;
                }
                if (moveDirection is not null)
                {
                    Log.Information("Moving");
                    Vector2 movePosition = Identity.PositionInRoom + moveDirection;
                    MoveCommand command = new(movePosition, Identity);
                    await command.ExecuteOnClient(this);
                }

                if (shouldUseWeapon)
                {
                    UseWeaponCommand command = new(Identity.Identity);
                    await command.ExecuteOnClient(this);
                }
            }

            await Task.Delay(10, token);
        }
    }


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
                    Log.Debug("Received server payload {payload}", jsonWrapperString);
                    PacketWrapper? wrapper = JsonSerializer.Deserialize<PacketWrapper>(jsonWrapperString, NetworkSerializer.Options);
                    if (wrapper is null || wrapper.TypeName is null || wrapper.JsonPayload is null)
                    {
                        Console.WriteLine("Malformed wrapper from server?");
                        continue;
                    }

                    Type? commandType = Type.GetType(wrapper.TypeName);
                    if (commandType is null)
                    {
                        Console.WriteLine($"Unknown command type from server: {wrapper.TypeName}");
                        continue;
                    }

                    if (JsonSerializer.Deserialize(wrapper.JsonPayload, commandType, NetworkSerializer.Options) is not ICommand command)
                    {
                        Console.WriteLine("Failed to deserialize command payload.");
                        continue;
                    }

                    Log.Information($"Executing command from server of type {command.GetType()}");
                    await command.ExecuteOnClient(this);

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
        Identity = players.Find((player) => player.Identity == identity);
        if (Identity is null)
        {
            throw new Exception($"bad server replication. could not find identity {identity}");
        }
        Log.Debug($"Identity updated to {identity}");
    }


    public void ApplySnapshot(GameStateSnapshot snapshot)
    {
        players = snapshot.Players;
        skeletons = snapshot.Skeletons;
        worldGrid = snapshot.WorldGrid;

        foreach (var room in worldGrid.GetAllRooms())
        {
            room.Occupants.Clear();
        }

        var allCharacters = players.Cast<Character>().Concat(skeletons);
        // fix references...
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
    }

    public async Task SendCommand(ICommand command)
    {
        try
        {
            Type commandType = command.GetType();
            var wrapper = new PacketWrapper
            {
                TypeName = commandType.AssemblyQualifiedName!,
                JsonPayload = JsonSerializer.Serialize(command, commandType, NetworkSerializer.Options)
            };

            byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(wrapper, NetworkSerializer.Options);

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
