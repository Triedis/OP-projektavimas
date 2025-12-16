using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Serilog;
using OP_Projektavimas.Utils;
using System.Diagnostics;
public class ServerStateController(int port) : IStateController
{
    // IStateController implementation
    public List<Player> players { get; set; } = [];
    public List<Enemy> enemies { get; set; } = [];
    public WorldGrid worldGrid { get; set; } = new(1337);

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
    public readonly PlayerStateCaretaker _playerStateCaretaker = new();


    public async Task Run()
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

            Player testPlayer = new("TestPlayer", Guid.NewGuid(), Color.Red, _, new Vector2(2, 2), new Sword(Guid.NewGuid(), 1, new PhysicalDamageEffect(1), "Test Sword"));
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
        IRoomFactory selectedFactory = PickFactoryFor(position);

        RoomCreationResult rootResult = selectedFactory.CreateRoom(position, worldGrid, rng);
        Room root = rootResult.Room;

        worldGrid.Rooms.Add(position, root);

        foreach (var enemy in rootResult.GeneratedEnemies)
        {
            EnqueueEnemySpawn(enemy);
        }

        ProcessPendingSpawns();
        var entry = LogEntry.ForGlobal($"A new area has been discovered: {root.GetType().Name}");

        MessageLog.Instance.Add(entry);
        _loggerChain?.Handle(entry);

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
        // Shuffled adjacent offsets for variety
        var offsets = new List<Vector2>
        {
            new(1, 0), new(-1, 0), new(0, 1), new(0, -1)
        }.OrderBy(_ => rng.Next());

        foreach (var off in offsets)
        {
            Vector2 candidate = parent.WorldGridPosition + off;
            if (!worldGrid.Rooms.ContainsKey(candidate))
                return candidate;
        }

        // Fallback: spiral search outwards from the parent
        int radius = 1;
        while (true)
        {
            radius++;
            for (int i = -radius; i <= radius; i++)
            {
                for (int j = -radius; j <= radius; j++)
                {
                    // To only check the perimeter of the square defined by the radius
                    if (Math.Abs(i) != radius && Math.Abs(j) != radius) continue;

                    Vector2 candidate = parent.WorldGridPosition + new Vector2(i, j);
                    if (!worldGrid.Rooms.ContainsKey(candidate))
                    {
                        return candidate;
                    }
                }
            }
            // As a safeguard against a completely full map, though unlikely
            if (radius > 50) throw new InvalidOperationException("Could not find a free position for a new room.");
        }
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
            var entry = LogEntry.ForRoom(
    $"Enemy {enemy.GetType().Name} has appeared!",
    enemy.Room
);

            MessageLog.Instance.Add(entry);
            _loggerChain?.Handle(entry);

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
                        var expiryEntry =
    LogEntry.ForPlayer($"{activeCommand} went away...", player);

                        MessageLog.Instance.Add(expiryEntry);
                        _loggerChain?.Handle(expiryEntry);

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

        var playerLeaveEntry =
            LogEntry.ForGlobal($"Player {player.Username} has departed.");

        MessageLog.Instance.Add(playerLeaveEntry);
        _loggerChain?.Handle(playerLeaveEntry);


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
        Sword starterWeapon = new(Guid.NewGuid(), 1, new PhysicalDamageEffect(10), "Starter Sword");
        // var vampiricEffect = new PhysicalDamageEffect(15); // It deals 15 damage
        // var starterWeapon = new VampiricSword(Guid.NewGuid(), 1, vampiricEffect, 0.5f); // 50% lifesteal
        Array colorValues = typeof(Color).GetEnumValues();
        Color randomColor = (Color?)colorValues.GetValue(rng.Next(colorValues.Length)) ?? throw new InvalidOperationException();
        Player player = new(username, identity, randomColor, initialRoom, middlePosition, starterWeapon);
        initialRoom.Enter(player);

        players.Add(player);

        player.OnDeath += (character) =>
        {
            if (character is Player p)
            {
                _playerStateCaretaker.Undo(p);
            }
        };

        var playerJoinEntry = LogEntry.ForGlobal($"Player {username} has appeared.");
        MessageLog.Instance.Add(playerJoinEntry);
        _loggerChain?.Handle(playerJoinEntry);
        return player;
    }


    public void EnqueueCommand(ICommand command)
    {
        _receivedCommands.Enqueue(command);
    }

    public Character? FindCharacterByIdentity(Guid identity)
    {
        return players.Cast<Character>().Concat(enemies).FirstOrDefault(c => c.Identity == identity);
    }
    //################# performanc testing #################
    string imagePath = Path.Combine(AppContext.BaseDirectory, "images", "orc.png");
    string filePath = "performance_results.txt";
    public void RunPerformanceTests()
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        _factories["Skeleton"] = new SkeletonFactory();
        _factories["Zombie"] = new ZombieFactory();
        _factories["Orc"] = new OrcFactory();
        _factories["Slime"] = new SlimeFactory();

        _standardRoomFactory = new StandardRoomFactory(_factories["Skeleton"]);
        _treasureRoomFactory = new TreasureRoomFactory(_factories["Orc"]);
        _bossRoomFactory = new BossRoomFactory(_factories["Orc"], _factories["Skeleton"]);



        Console.WriteLine("=== Performance Test ===");
        Room testRoom = CreateInitialRoom(); // create a single room for testing

        int enemyCount = 1000; // number of enemies to spawn
        Console.WriteLine("Testing Non-Flyweight...");
        MeasureEnemySpawnPerformanceNonFlyweight(enemyCount, testRoom);

        Console.WriteLine("Testing Flyweight...");
        MeasureEnemySpawnPerformanceFlyweight(enemyCount, testRoom);

        Console.WriteLine("=== End of Test ===");
        Console.WriteLine($"Result file: {filePath}");
        ClearServerState();
    }

    private void MeasureEnemySpawnPerformanceNonFlyweight(int count, Room testRoom)
    {
        // Clear enemies
        enemies.Clear();
        _pendingSpawns.Clear();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        long memoryBefore = GC.GetTotalMemory(true);

        Stopwatch sw = Stopwatch.StartNew();

        for (int i = 0; i < count; i++)
        {
            // Each enemy loads its own image (non-flyweight)
            Enemy e = new OrcTest(Guid.NewGuid(), testRoom, new Vector2(0, 0), new Axe(Guid.NewGuid(), 1, new PhysicalDamageEffect(1)), imagePath);
            EnqueueEnemySpawn(e);
        }

        ProcessPendingSpawns();

        sw.Stop();
        long memoryAfter = GC.GetTotalMemory(true);

        Console.WriteLine($"Non-Flyweight: Spawned {count} enemies in {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"Memory used: {(memoryAfter - memoryBefore) / 1024.0:F2} KB");
        LogResult($"Non-Flyweight: Spawned {count} enemies in {sw.ElapsedMilliseconds} ms\n Memory used: {(memoryAfter - memoryBefore) / 1024.0:F2} KB");
    }

    private void MeasureEnemySpawnPerformanceFlyweight(int count, Room testRoom)
    {
        // Clear enemies
        enemies.Clear();
        _pendingSpawns.Clear();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        long memoryBefore = GC.GetTotalMemory(true);

        // Flyweight: share the image bytes among all enemies

        Stopwatch sw = Stopwatch.StartNew();

        for (int i = 0; i < count; i++)
        {
            Enemy e = new OrcTest(Guid.NewGuid(), testRoom, new Vector2(0, 0), new Axe(Guid.NewGuid(), 1, new PhysicalDamageEffect(1)), imagePath, true);
            EnqueueEnemySpawn(e);
        }

        ProcessPendingSpawns();

        sw.Stop();
        long memoryAfter = GC.GetTotalMemory(true);

        Console.WriteLine($"Flyweight: Spawned {count} enemies in {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"Memory used: {(memoryAfter - memoryBefore) / 1024.0:F2} KB");
        LogResult($"Flyweight: Spawned {count} enemies in {sw.ElapsedMilliseconds} ms\n Memory used: {(memoryAfter - memoryBefore) / 1024.0:F2} KB");
    }

    void LogResult(string text)
    {
        File.AppendAllText(filePath, text + Environment.NewLine);
    }
    private void ClearServerState()
    {
        // Clear enemies and pending spawns
        enemies.Clear();
        _pendingSpawns.Clear();
        _factories.Clear();

        _standardRoomFactory = null;
        _bossRoomFactory = null;
        _treasureRoomFactory = null;

        EnemyImageFlyweight.ClearCache();

        // Optional: reset world rooms or other state if generated during tests
        worldGrid.Rooms.Clear();

        Console.WriteLine("Server state cleared after performance tests.");
    }
}
