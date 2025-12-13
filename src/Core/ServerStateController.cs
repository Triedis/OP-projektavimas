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
    //private readonly MessageLog _log;
    // should be a singleton for consistency.
    private readonly Random rng = new();
    private readonly Vector2 _initialRoomPosition = new(0, 0);
    private PlayerEnemyAdapter adaptedEnemy;
    private readonly Dictionary<string, IEnemyFactory> _factories = new();

    private IRoomFactory _standardRoomFactory;
    private IRoomFactory _treasureRoomFactory;
    private IRoomFactory _bossRoomFactory;
    private GameLoggerHandler? _loggerChain;


    public override async Task Run()
    {
        try
        {
            var firstPlayerId = Guid.NewGuid(); // could be a test player or use placeholder
            var initialRoomPos = _initialRoomPosition;

            var globalLogger = new GlobalLogger();
            var playerLogger = new PlayerLogger(firstPlayerId);
            var roomLogger = new RoomLogger(initialRoomPos);
            var allLogger = new AllLogger();

            globalLogger.SetNext(playerLogger);
            playerLogger.SetNext(roomLogger);
            roomLogger.SetNext(allLogger);

            _loggerChain = globalLogger;

            _factories["Skeleton"] = new SkeletonFactory();
            _factories["Zombie"] = new ZombieFactory();
            _factories["Orc"] = new OrcFactory();
            _factories["Slime"] = new SlimeFactory();

            _standardRoomFactory = new StandardRoomFactory(_factories["Skeleton"]);
            _treasureRoomFactory = new TreasureRoomFactory(_factories["Orc"]);
            _bossRoomFactory = new BossRoomFactory(_factories["Orc"], _factories["Skeleton"]);

            Room _ = CreateInitialRoom();
            worldGrid.PrintRoomTree(_);

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

            try
            {
                RunAI();
                TickOngoingEffects();
                ProcessPendingSpawns();
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
    private void RunAI()
    {
        Log.Debug("Ticking AI with {count} entities", enemies.Count);
        foreach (Enemy enemy in enemies)
        {
            if (enemy.GetType() != typeof(Player))
            {
                ICommand? command = enemy.TickAI();

                if (command is not null)
                {
                    Log.Debug("Entity {enemy} decided to {command}", enemy, command.GetType());
                }
                command?.ExecuteOnServer(this);
            }
        }
    }
    public Room CreateInitialRoom()
    {
        return CreateAndPopulateRoom(_initialRoomPosition);
    }
    public Room CreateAndPopulateRoom(Vector2 position)
    {
        // If the world already has rooms, return the root (first room)
        if (worldGrid.Rooms.Count > 0)
            return worldGrid.Rooms.Values.First();

        // ---- CREATE ROOT ROOM (same as you had) ----
        IRoomFactory selectedFactory = PickFactoryFor(position);

        RoomCreationResult rootResult = selectedFactory.CreateRoom(position, worldGrid, rng);
        Room root = rootResult.Room;

        worldGrid.Rooms.Add(position, root);

        foreach (var enemy in rootResult.GeneratedEnemies)
            EnqueueEnemySpawn(enemy);

        ProcessPendingSpawns();
        MessageLog.Instance.Add(LogEntry.ForGlobal($"A new area has been discovered: {root.GetType().Name}"));

        // ---- NEW: generate a full Composite dungeon ----
        GenerateChildren(root, depth: 3);   // 3 levels deep, tweak as needed

        return root;
    }
    private IRoomFactory PickFactoryFor(Vector2 position)
    {
        double chance = rng.NextDouble();
        int roomCount = worldGrid.Rooms.Count;

        if (chance < 0.6 && roomCount > 1)
            return _treasureRoomFactory;

        if (chance < 0.3 && roomCount > 3)
            return _bossRoomFactory;

        return _standardRoomFactory;
    }
    private void GenerateChildren(Room parent, int depth)
    {
        if (depth <= 0)
            return;

        int childCount = rng.Next(1, 4); // 1–3 new rooms per room

        for (int i = 0; i < childCount; i++)
        {
            Vector2 newPos = FindFreePositionNear(parent);

            IRoomFactory factory = PickFactoryFor(newPos);
            RoomCreationResult result = factory.CreateRoom(newPos, worldGrid, rng);
            Room child = result.Room;

            // Composite connection first
            parent.Add(child);

            // Then add to world grid
            worldGrid.Rooms.Add(newPos, child);

            // Populate enemies
            foreach (var e in result.GeneratedEnemies)
                EnqueueEnemySpawn(e);

            ProcessPendingSpawns();

            // Recurse
            GenerateChildren(child, depth - 1);
        }
    }
    private Vector2 FindFreePositionNear(Room parent)
    {
        // Adjacent offsets
        Vector2[] offsets =
        {
        new Vector2(1, 0),
        new Vector2(-1, 0),
        new Vector2(0, 1),
        new Vector2(0, -1)
    };

        foreach (var off in offsets)
        {
            Vector2 candidate = parent.WorldGridPosition + off;
            if (!worldGrid.Rooms.ContainsKey(candidate))
                return candidate;
        }

        // Fallback random position
        return parent.WorldGridPosition + new Vector2(rng.Next(-5, 6), rng.Next(-5, 6));
    }

    private readonly Queue<Enemy> _pendingSpawns = new();

    public void EnqueueEnemySpawn(Enemy enemy)
    {
        _pendingSpawns.Enqueue(enemy);
    }

    // Call this **after RunAI()** in your game loop:
    public void ProcessPendingSpawns()
    {
        while (_pendingSpawns.Count > 0)
        {
            Enemy enemy = _pendingSpawns.Dequeue();
            enemies.Add(enemy);
            enemy.Room.Enter(enemy);
            Log.Information("Enemy {enemy} actually spawned in room {room}", enemy, enemy.Room);
        }
    }
    private readonly List<IStatus> _ongoingEffects = new();
    public void RegisterOngoingEffect(IStatus effect)
    {
        _ongoingEffects.Add(effect);
    }
    public void TickOngoingEffects()
    {
        Log.Debug("Ticking with {count} effects", _ongoingEffects.Count);
        for (int i = _ongoingEffects.Count - 1; i >= 0; i--)
        {
            var effect = _ongoingEffects[i];
            if (!effect.Tick())
            {
                _ongoingEffects.RemoveAt(i);
            }
        }

        IEnumerable<Character> characters = players.Concat<Character>(enemies);
        foreach (var character in characters)
        {
            var activeCmds = character.ActiveCommands.ToList(); // explicit copy
            foreach (IActionCommand activeCommand in activeCmds)
            {
                if (activeCommand.Expired())
                {
                    if (character is Player player && !player.Dead)
                    {
                        LogEntry expiryEntry = LogEntry.ForPlayer($"{activeCommand} went away...", player);
                        MessageLog.Instance.Add(expiryEntry);
                    }
                    activeCommand.Undo(this);
                    character.ActiveCommands.Remove(activeCommand);
                }
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

        Player player = AddPlayer(Guid.NewGuid(), username);
        Guid clientIdentity = player.Identity;

        // Register for replication
        _clients.Add(clientIdentity, client.GetStream());
        var joinEntry = LogEntry.ForGlobal($"Received client connection ... {clientIdentity}");
        MessageLog.Instance.Add(joinEntry);
        _loggerChain.Handle(joinEntry);

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
    public Player AddPlayer(System.Guid identity, string username)
    {
        Room? initialRoom = worldGrid.GetRoom(_initialRoomPosition);
        if (initialRoom is null)
        {
            //Log.Error("Initial room missing?");
            throw new InvalidOperationException();
        }

        Vector2 initialRoomShape = initialRoom.Shape;
        Vector2 middlePosition = new(initialRoomShape.X / 2, initialRoomShape.Y / 2);
        Sword starterWeapon = new(Guid.NewGuid(), 1, new PhysicalDamageEffect(10));
        // var vampiricEffect = new PhysicalDamageEffect(15); // It deals 15 damage
        // var starterWeapon = new VampiricSword(Guid.NewGuid(), 1, vampiricEffect, 0.5f); // 50% lifesteal
        Array colorValues = typeof(Color).GetEnumValues();
        Color randomColor = (Color?)colorValues.GetValue(rng.Next(colorValues.Length)) ?? throw new InvalidOperationException();
        Player player = new(username, identity, randomColor, initialRoom, middlePosition, starterWeapon);
        initialRoom.Enter(player);

        players.Add(player);

        LogEntry playerJoinEntry = LogEntry.ForGlobal($"Player {username} has appeared.");
        MessageLog.Instance.Add(playerJoinEntry);

        return player;
    }


    public void EnqueueCommand(ICommand command)
    {
        _receivedCommands.Enqueue(command);
    }
}
