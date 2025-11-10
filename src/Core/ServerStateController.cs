using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Serilog;
using OP_Projektavimas.Utils;
class ServerStateController(int port) : IStateController
{
    private readonly int _port = port;
    private readonly Queue<ICommand> _receivedCommands = [];
    private readonly Dictionary<Guid, NetworkStream> _clients = [];
    // should be a singleton for consistency.
    private PlayerEnemyAdapter adaptedEnemy;
    private GameFacade? _game;
    public GameFacade Game => _game ?? throw new InvalidOperationException("GameFacade not initialized");

    public override async Task Run()
    {
        try
        {
            _game = new(this, worldGrid, MessageLog.Instance);
            Room _ = _game.CreateInitialRoom();

            //moved to GameFacade
            // _game.SpawnEnemy("Zombie", _, new(1, 1));
            // _game.SpawnEnemy("Skeleton", _, new(2, 2));
            // _game.SpawnEnemy("Orc", _, new(3, 3));
            // _game.SpawnEnemy("Slime", _, new(4, 4));

            Player testPlayer = new("TestPlayer", Guid.NewGuid(), Color.Red, _, new Vector2(2, 2), new Sword(Guid.NewGuid(), 1, new PhysicalDamageEffect(1)));
            //_.Enter(testPlayer);
            adaptedEnemy = new(testPlayer, _);
            adaptedEnemy.SetStrategy(new MeleeStrategy()); // start with melee
            //players.Add(adaptedEnemy); //jeigu atkomentuoju šitą kliento gui nebeloadina
            //Log.Information(adaptedEnemy.Room.ToString());
            _.Enter(adaptedEnemy);

            var clientTask = ListenForClients();
            var serverTask = GameLoop();

            await Task.WhenAll(clientTask, serverTask);
        }
        catch (Exception e)
        {
            Console.WriteLine($"?: {e}");
        }
    }

    private async Task GameLoop()
    {
        int count = 0;
        while (true)
        {
            //if (count == 20)
            //{
            //    adaptedEnemy.Weapon = new Bow(3, 1, Guid.NewGuid()); // give it a bow
            //    adaptedEnemy.SetStrategy(new RangedStrategy());
            //    Log.Information("{enemy} switched to RangedStrategy!", adaptedEnemy);
            //}

            //AI, status effects and spawning moved to game facade
            try
            {
                _game.Run();
                await ExecuteClientCommands();
                count++;
                await SyncAll(); // ideally should be a delta update ...
                await Task.Delay(500);
            }
            catch (Exception ex)
            {
                Log.Error("{ex}", ex);
            }
        }
    }

    private async Task ExecuteClientCommands()
    {
        if (_receivedCommands.Count > 0)
        {
            while (_receivedCommands.Count > 0)
            {
                try
                {
                    ICommand command = _receivedCommands.Dequeue();
                    Log.Information($"Executing {command.GetType()}");

                    await command.ExecuteOnServer(this);

                }
                catch (Exception e)
                {
                    Log.Error($"Failed to execute command from queue: {e}");
                }
            }


        }

    }

    private async Task SendCommand(ICommand command, NetworkStream clientStream)
    {
        Log.Debug($"Sending of type {command.GetType()}");

        var duplicateGroups = worldGrid.GetAllRooms()
            .GroupBy(room => room.WorldGridPosition)
            .Where(group => group.Count() > 1);

        if (duplicateGroups.Any())
        {
            foreach (var group in duplicateGroups)
            {
                Log.Warning($"Duplicate room found at position {group.Key}. Count: {group.Count()}");
            }
        }

        byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(command, NetworkSerializer.Options);

        int messageLength = jsonBytes.Length; // required for length-prefixing. TCP can be annoying.
        byte[] lengthPrefix = BitConverter.GetBytes(messageLength);

        var b = Encoding.UTF8.GetString(jsonBytes, 0, messageLength);
        // Log.Debug("sending {b}", b);

        try
        {
            await clientStream.WriteAsync(lengthPrefix);
            await clientStream.WriteAsync(jsonBytes);
            await clientStream.FlushAsync();
        }
        catch
        {
            Log.Debug($"Failed to repliate to client. Is it still alive?");
            throw;
        }

        Log.Debug($"Sent of type {command.GetType()} with length {messageLength}");
    }

    private async Task ListenForClients()
    {

        TcpListener listener = new(IPAddress.Any, _port);
        listener.Start();

        List<Task> clientTasks = [];

        Log.Information($"Running TCP server on {_port}");
        while (true)
        {
            TcpClient client = await listener.AcceptTcpClientAsync();
            clientTasks.Add(HandleClient(client));
        }
    }

    public async Task HandleClient(TcpClient client)
    {
        string? username = client.Client.RemoteEndPoint?.ToString();
        if (username is null)
        {
            return;
        }

        Player player = _game.AddPlayer(Guid.NewGuid(), username);
        Guid clientIdentity = player.Identity;

        // Register for replication
        _clients.Add(clientIdentity, client.GetStream());
        Log.Information($"Received client connection ... {clientIdentity}");

        try
        {
            await ListenForClient(client);
        }
        catch (Exception e)
        {
            Log.Warning("Looks like {e} really would just brick the server ...", e);
        }
    }

    public async Task SyncAll()
    {
        foreach (KeyValuePair<Guid, NetworkStream> entry in _clients)
        {
            Guid clientIdentity = entry.Key;
            NetworkStream clientStream = entry.Value;

            SyncCommand cmd = GetSnapshotCommand(clientIdentity);
            try
            {
                await SendCommand(cmd, clientStream);
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to replicate to client {client}. Disconnecting.", clientIdentity);
                Disconnect(clientIdentity);
            }
        }
    }

    private void Disconnect(System.Guid identity)
    {
        Player? player = players.Where(player => player.Identity == identity).FirstOrDefault();

        if (player is null)
        {
            Log.Warning("Attempted to disconnect client {identity} which is not bound to any player object");
            return;
        }

        LogEntry playerLeaveEntry = LogEntry.ForGlobal($"Player {player.Username} has departed.");
        MessageLog.Instance.Add(playerLeaveEntry);

        player.Destroy(); // destroy is called to remove the character from any rooms
        players.Remove(player);
        _clients.Remove(identity);
    }


    private SyncCommand GetSnapshotCommand(System.Guid clientIdentity)
    {
        IReadOnlyList<LogEntry> serverLogEntries = MessageLog.Instance.GetAllMessages();
        List<LogEntryDto> logDtos = serverLogEntries.Select(entry => new LogEntryDto
        {
            Text = entry.Text,
            Scope = entry.Scope,
            PlayerIdentity = entry.PlayerIdentity?.Identity,
            RoomPosition = entry.RoomPosition
        }).ToList();

        GameStateSnapshot snapshot = new(players, enemies, worldGrid, logDtos);
        return new SyncCommand(snapshot, clientIdentity);
    }

    private async Task ListenForClient(TcpClient client)
    {
        var _log = Log.ForContext("client", client.Client.RemoteEndPoint);
        _log.Debug("Listening ...");

        NetworkStream stream = client.GetStream();

        byte[] lengthBuffer = new byte[4];

        while (true)
        {
            await stream.ReadExactlyAsync(lengthBuffer, 0, 4);
            int messageLength = BitConverter.ToInt32(lengthBuffer, 0);

            byte[] messageBuffer = new byte[messageLength];
            await stream.ReadExactlyAsync(messageBuffer, 0, messageLength);

            string jsonCommandString = Encoding.UTF8.GetString(messageBuffer);
            try
            {
                ICommand? command = JsonSerializer.Deserialize<ICommand>(jsonCommandString, NetworkSerializer.Options);
                if (command is null)
                {
                    _log.Warning("Malformed command from client?");
                    continue;
                }

                _log.Debug($"Executing command from client of type {command.GetType()}");
                EnqueueCommand(command);
            }
            catch (JsonException e)
            {
                _log.Warning(e, "Failed to deserialize client packet.");
            }
        }
    }


    private void EnqueueCommand(ICommand command)
    {
        _receivedCommands.Enqueue(command);
    }
}
