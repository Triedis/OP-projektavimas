using Serilog;

class GameFacade
{
    private readonly ServerStateController _server;
    private readonly WorldGrid _world;
    private readonly MessageLog _log;
    private readonly Random rng = new();
    private readonly Vector2 _initialRoomPosition = new(0, 0);
    private readonly Dictionary<string, IEnemyFactory> _factories = new();

    public GameFacade(ServerStateController server, WorldGrid worldGrid, MessageLog log)
    {
        this._server = server;
        this._world = worldGrid;
        this._log = log;

        _factories["Skeleton"] = new SkeletonFactory();
        _factories["Zombie"] = new ZombieFactory();
        _factories["Orc"] = new OrcFactory();
        _factories["Slime"] = new SlimeFactory();
    }

    public void SpawnEnemy(string type, Room room, Vector2 position)
    {
        if (!_factories.TryGetValue(type, out var factory))
        {
            _log.Add(LogEntry.ForGlobal($"Enemy type {type} is unknown!"));
            return;
        }

        if (room == null)
        {
            _log.Add(LogEntry.ForGlobal("No room available for spawn."));
            return;
        }

        Enemy enemy = factory.CreateEnemy(room, position);
        EnqueueEnemySpawn(enemy);
        _log.Add(LogEntry.ForGlobal($"{type} spawned at {position}."));
    }
    public Room CreateInitialRoom()
    {
        Room _ = _world.GenRoom(_initialRoomPosition);

        SpawnEnemy("Zombie", _, new(1, 1));
        //SpawnEnemy("Skeleton", _, new(2, 2));
        SpawnEnemy("Orc", _, new(3, 3));
        SpawnEnemy("Slime", _, new(4, 4));
        CreateLootDrop(new Axe(Guid.NewGuid(), 1, new PhysicalDamageEffect(10)), _, new(2, 2));
        return _;
    }
    public LootDrop CreateLootDrop(Weapon item, Room room, Vector2 pos)
    {
        LootDrop loot = new LootDrop(item, pos);
        room.LootDrops.Add(loot);
        return loot;
    }
    public LootDrop CreateRandomLootDrop(Room room, Vector2 pos)
    {
        if (_factories.Count == 0)
        {
            throw new InvalidOperationException("No enemy factories registered!");
        }

        var randomIndex = new Random().Next(_factories.Count);
        var factory = _factories.ElementAt(randomIndex).Value;
        Weapon weapon = factory.CreateWeapon();

        _log.Add(LogEntry.ForGlobal($"Random weapon generated: {weapon}"));
        return CreateLootDrop(weapon, room, pos);
    }
    public async Task UseWeapon(Guid actorId)
    {
        Log.Information("UseWeaponCommand::ExecuteOnServer from {actorIdentity}", actorId);

        Character? actor = _server.players
    .Concat(_server.enemies.Cast<Character>())
    .FirstOrDefault(c => c.Identity == actorId);

        if (actor is null)
        {
            Log.Warning("UseWeaponCommand's ActorIdentity is not bound to any character object");
            return;
        }
        if (actor.Dead)
        {
            Log.Debug("Dead player {id} tried to act. Ignoring.", actor.Identity);
            return;
        }
        Weapon weapon = actor.Weapon;

        Room room = actor.Room;
        Character? target = actor.GetClosestOpponent();
        if (target is null)
        {
            Log.Debug("UseWeaponCommand has no suitable target");
            return;
        }

        if (weapon.CanUse(actor, target, _server))
        {
            Log.Information("Weapon acting on {tgt} {id}", target, target.Identity);
            LogEntry weaponUseLogEntry = LogEntry.ForRoom($"{actor} swings {actor.Weapon} and hits {target}", room);
            _log.Add(weaponUseLogEntry);

            IReadOnlyList<IActionCommand> consequencues = weapon.Act(actor, target);
            foreach (IActionCommand consequence in consequencues)
            {
                consequence.Execute(_server);
            }
        }
    }
    public void Run()
    {
        //CreateInitialRoom();
        RunAI();
        TickOngoingEffects();
        ProcessPendingSpawns();
    }
    private void RunAI()
    {
        Log.Debug("Ticking AI with {count} entities", _server.enemies.Count);
        foreach (Enemy enemy in _server.enemies)
        {
            if (enemy.GetType() != typeof(Player))
            {
                ICommand? command = enemy.TickAI();

                if (command is not null)
                {
                    Log.Debug("Entity {enemy} decided to {command}", enemy, command.GetType());
                }
                command?.ExecuteOnServer(_server);
            }
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
            _server.enemies.Add(enemy);
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
    }

    public async Task MovePlayer(Guid playerId, Vector2 newPosition)
    {
        await Task.Run(() =>
        {
            Character? target = _server.players.Cast<Character>().Concat(_server.enemies).FirstOrDefault((character) => character.Identity.Equals(playerId));
            if (target is null)
            {
                Console.WriteLine("Failed to replicate movement on server. Nil.");
                return;
            }
            if (target.Dead)
            {
                Console.WriteLine("Actor is dead and cant move");
                return;
            }
            var pos = newPosition;
            var room = target.Room;
            var roomx = room.Shape.X;
            var roomy = room.Shape.Y;

            Console.WriteLine($"MoveCommand exec: pos={pos},char={playerId},rs={room.Shape}");

            bool inBoundsX = pos.X >= 1 && pos.X <= roomx - 2;
            bool inBoundsY = pos.Y >= 1 && pos.Y <= roomy - 2;
            bool inBounds = inBoundsX && inBoundsY;

            KeyValuePair<Direction, RoomBoundary> exitDirValuePair = room.BoundaryPoints.Where(pair => pair.Value.PositionInRoom.Equals(pos)).FirstOrDefault();

            Log.Debug("Movement in bounds? {inBounds}", inBounds);
            Log.Debug("Moving to exit? {exitDirValuePair}", exitDirValuePair.Value is not null);
            if (inBounds)
            {
                IEnumerable<Character> occupants = room.Occupants;
                bool clear = true;
                foreach (Character occupant in occupants)
                {
                    if (occupant.Identity.Equals(playerId))
                    {
                        continue;
                    }

                    if (occupant.PositionInRoom.Equals(pos) && !occupant.Dead)
                    {
                        clear = false;
                    }
                }

                if (clear)
                {
                    Log.Debug("Moving in room to {pos}", pos);
                    target.SetPositionInRoom(pos);
                    Console.ForegroundColor = ConsoleColor.White;
                }

            }
            else if (exitDirValuePair.Value is not null)
            {
                Vector2 offsetPosition = room.WorldGridPosition + DirectionUtils.GetVectorDirection(exitDirValuePair.Key);
                Console.WriteLine($"Trying to enter room at {offsetPosition}");
                Room? newRoom = _world.GetRoom(offsetPosition);
                if (newRoom is null)
                {

                    Console.WriteLine("Room is null ... Creating.");
                    newRoom = _world.GenRoom(offsetPosition);
                }
                Direction enteringFrom = DirectionUtils.GetOpposite(exitDirValuePair.Key);
                Console.WriteLine($"Entering room from {enteringFrom}");
                newRoom.Enter(target, enteringFrom);

                return;
            }
        });
    }
    public Player AddPlayer(System.Guid identity, string username)
    {
        Room? initialRoom = _world.GetRoom(_initialRoomPosition);
        if (initialRoom is null)
        {
            Log.Error("Initial room missing?");
            throw new InvalidOperationException();
        }

        Vector2 initialRoomShape = initialRoom.Shape;
        Vector2 middlePosition = new(initialRoomShape.X / 2, initialRoomShape.Y / 2);
        Sword starterWeapon = new(Guid.NewGuid(), 1, new PhysicalDamageEffect(10));
        Array colorValues = typeof(Color).GetEnumValues();
        Color randomColor = (Color?)colorValues.GetValue(rng.Next(colorValues.Length)) ?? throw new InvalidOperationException();
        Player player = new(username, identity, randomColor, initialRoom, middlePosition, starterWeapon);
        initialRoom.Enter(player);

        _server.players.Add(player);

        LogEntry playerJoinEntry = LogEntry.ForGlobal($"Player {username} has appeared.");
        MessageLog.Instance.Add(playerJoinEntry);

        return player;
    }
}