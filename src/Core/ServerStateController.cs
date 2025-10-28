using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Serilog;

class ServerStateController(int port) : IStateController
{
    private readonly Vector2 _initialRoomPosition = new(0, 0);
    private readonly int _port = port;
    private readonly Queue<ICommand> _receivedCommands = [];
    private readonly Dictionary<Guid, NetworkStream> _clients = [];
    private readonly Random rng = new(); // should be a singleton for consistency.


    public override async Task Run()
    {
        try
        {
            IEnemyFactory factory1 = new SkeletonFactory();
            IEnemyFactory factory2 = new OrcFactory();
            IEnemyFactory factory3 = new ZombieFactory();
            Room _ = worldGrid.GenRoom(_initialRoomPosition);
            //Bow skeletonSword = new(3, 10, Guid.NewGuid());
            // Enemy testSkeleton = factory1.CreateEnemy(_, new(2, 2));
            // enemies.Add(testSkeleton);
            // _.Enter(testSkeleton);

            //Enemy testOrc = factory2.CreateEnemy(_, new(3, 3));
            //enemies.Add(testOrc);
            //_.Enter(testOrc);

            Enemy testZombie = factory3.CreateEnemy(_, new(4, 4));
            enemies.Add(testZombie);
            _.Enter(testZombie);
            //Skeleton testSkeleton = new(System.Guid.NewGuid(), _, new(2, 2), skeletonSword);
            //enemies.Add(testSkeleton);
            //_.Enter(testSkeleton);


            Sword slimeSword = new(1, 10, Guid.NewGuid());
            Slime testSlime = new(System.Guid.NewGuid(), _, new(3, 3), slimeSword);
            enemies.Add(testSlime);
            _.Enter(testSlime);


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

        while (true)
        {
            RunAI();
            ProcessPendingSpawns();
            await ExecuteClientCommands();

            await SyncAll(); // ideally should be a delta update ...
            await Task.Delay(500);
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
    private readonly Queue<Enemy> _pendingSpawns = new();

    public void EnqueueEnemySpawn(Enemy enemy)
    {
        _pendingSpawns.Enqueue(enemy);
    }

    // Call this **after RunAI()** in your game loop:
    private void ProcessPendingSpawns()
    {
        while (_pendingSpawns.Count > 0)
        {
            Enemy enemy = _pendingSpawns.Dequeue();
            enemies.Add(enemy);
            enemy.Room.Enter(enemy);
            Log.Information("Enemy {enemy} actually spawned in room {room}", enemy, enemy.Room);
        }
    }

    private void RunAI()
    {
        Log.Debug("Ticking AI with {count} entities", enemies.Count);
        foreach (Enemy enemy in enemies)
        {
            ICommand? command = enemy.TickAI();

            if (command is not null)
            {
                Log.Debug("Entity {enemy} decided to {command}", enemy, command.GetType());
            }
            command?.ExecuteOnServer(this);
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
            Log.Debug("sending {b}", b);

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

        Player player = AddPlayer(Guid.NewGuid(), username);
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

    private async Task SyncAll()
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
            catch
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

    private Player AddPlayer(System.Guid identity, string username)
    {
        Room? initialRoom = worldGrid.GetRoom(_initialRoomPosition);
        if (initialRoom is null)
        {
            Log.Error("Initial room missing?");
            throw new InvalidOperationException();
        }

        Vector2 initialRoomShape = initialRoom.Shape;
        Vector2 middlePosition = new(initialRoomShape.X / 2, initialRoomShape.Y / 2);
        Sword starterWeapon = new(1, 25, Guid.NewGuid());
        Array colorValues = typeof(Color).GetEnumValues();
        Color randomColor = (Color?)colorValues.GetValue(rng.Next(colorValues.Length)) ?? throw new InvalidOperationException();
        Player player = new(username, identity, randomColor, initialRoom, middlePosition, starterWeapon);
        initialRoom.Enter(player);

        players.Add(player);

        LogEntry playerJoinEntry = LogEntry.ForGlobal($"Player {username} has appeared.");
        MessageLog.Instance.Add(playerJoinEntry);

        return player;
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
