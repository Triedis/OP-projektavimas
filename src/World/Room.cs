using DungeonCrawler.src.Iterators;
using Serilog;
using System.Text.Json.Serialization;

[JsonDerivedType(typeof(StandardRoom), typeDiscriminator: "StandardRoom")]
[JsonDerivedType(typeof(TreasureRoom), typeDiscriminator: "TreasureRoom")]
[JsonDerivedType(typeof(BossRoom), typeDiscriminator: "BossRoom")]
public abstract class Room : IRoomComposite, IVisitableRoom, IterableCollection<Character>
{
    public abstract void Accept(IRoomVisitor visitor);

    public Vector2 WorldGridPosition { get; }
    public Vector2 Shape { get; protected set; }
    public List<LootDrop> LootDrops { get; protected set; } = [];
    protected List<Room> _children = new();
    public Room? ParentRoom { get; set; }


    public virtual void Add(Room child) => _children.Add(child);
    public virtual void Remove(Room child) => _children.Remove(child);
    public IReadOnlyList<Room> GetChildren() => _children;


    [JsonIgnore]
    public List<Character> Occupants { get; private set; } = [];
    public Dictionary<Direction, RoomBoundary> BoundaryPoints { get; protected set; } = [];
    protected Room() { }

    protected Room(Vector2 worldGridPosition, WorldGrid world)
    {
        WorldGridPosition = worldGridPosition;
        LootDrops = [];
        BoundaryPoints = [];
        // Shape and such must be initialized by a subclass
    }

    [JsonConstructor]
    public Room(Vector2 worldGridPosition, Vector2 shape, List<LootDrop> lootDrops, Dictionary<Direction, RoomBoundary> boundaryPoints)
    {
        WorldGridPosition = worldGridPosition;
        Shape = shape;
        LootDrops = lootDrops;
        BoundaryPoints = boundaryPoints;
    }
    public Iterator<Character> CreateIterator()
    {
        return new RoomCharacterIterator(Occupants);
    }
    protected void InitializeBoundaries(WorldGrid world, int minExits, int maxExits)
    {
        Dictionary<Direction, Room?> locality = new()
        {
            { Direction.NORTH, world.GetRoom(WorldGridPosition + DirectionUtils.GetVectorDirection(Direction.NORTH)) },
            { Direction.EAST, world.GetRoom(WorldGridPosition + DirectionUtils.GetVectorDirection(Direction.EAST)) },
            { Direction.SOUTH, world.GetRoom(WorldGridPosition + DirectionUtils.GetVectorDirection(Direction.SOUTH)) },
            { Direction.WEST, world.GetRoom(WorldGridPosition + DirectionUtils.GetVectorDirection(Direction.WEST)) }
        };

        Dictionary<Direction, bool> used = new();
        int existingConnections = 0;

        foreach (Direction dir in Enum.GetValues<Direction>())
        {
            used[dir] = false;
            if (locality.TryGetValue(dir, out Room? adjacent) && adjacent != null && adjacent.BoundaryPoints.ContainsKey(DirectionUtils.GetOpposite(dir)))
            {
                BoundaryPoints[dir] = CreateBoundaryForDirection(dir);
                existingConnections++;
                used[dir] = true;
            }
        }

        int numBoundariesToGenerate = world.random.Next(minExits, maxExits + 1) - existingConnections;
        for (int i = 0; i < numBoundariesToGenerate; i++)
        {
            var unusedDirections = used.Where(pair => !pair.Value).Select(pair => pair.Key).ToList();
            if (unusedDirections.Count == 0) break;

            Direction directionToUse = unusedDirections[world.random.Next(unusedDirections.Count)];
            BoundaryPoints[directionToUse] = CreateBoundaryForDirection(directionToUse);
            used[directionToUse] = true;
        }
    }

    protected RoomBoundary CreateBoundaryForDirection(Direction dir)
    {
        Vector2 position = dir switch
        {
            Direction.NORTH => new Vector2(Shape.X / 2, 0),
            Direction.EAST => new Vector2(Shape.X - 1, Shape.Y / 2),
            Direction.SOUTH => new Vector2(Shape.X / 2, Shape.Y - 1),
            Direction.WEST => new Vector2(0, Shape.Y / 2),
            _ => new Vector2(Shape.X / 2, Shape.Y / 2),
        };
        return new RoomBoundary(position);
    }
    public virtual void Enter(Character character)
    {
        character.Room.Exit(character);
        character.EnterRoom(this);
        Occupants.Add(character);
    }
    public virtual void Enter(Character character, Direction enteringFrom) {
        Enter(character);

        RoomBoundary? entryPoint = BoundaryPoints.GetValueOrDefault(enteringFrom);
        if (entryPoint is null)
        {
            Log.Error("[Room.Enter] Character {ActorID} tried to enter room {RoomID} from direction {EnteringFrom}, but no boundary exists.",
                character.Identity, WorldGridPosition, enteringFrom);
            throw new Exception("Character entered a room from a direction with no boundary.");
        }

        var newPosition = entryPoint.PositionInRoom
            + DirectionUtils.GetVectorDirection(
                DirectionUtils.GetOpposite(enteringFrom)
            );
        
        character.SetPositionInRoom(newPosition);

        Log.Information("[Room.Enter] Placed character {ActorID} at new position {Position} in room {RoomID}",
            character.Identity, newPosition, WorldGridPosition);
    }

    public virtual void Exit(Character character)
    {
        Occupants.Remove(character);
    }
    
    // Common base class logic
    public override bool Equals(object? obj)
    {
        return obj is Room room && room.WorldGridPosition.Equals(WorldGridPosition);
    }
    public override int GetHashCode()
    {
        return this.WorldGridPosition.GetHashCode();
    }

    public override string? ToString()
    {
        return WorldGridPosition.ToString();
    }
}
public abstract class SafeRoom : Room
{
    public abstract override void Accept(IRoomVisitor visitor);
    public SafeRoom(Vector2 worldGridPosition, WorldGrid world)
    : base(worldGridPosition, world) // call Room's constructor
    {
    }
    public SafeRoom(Vector2 worldGridPosition, WorldGrid world, Random rng)
    : base(worldGridPosition, world)
    {
        // you can use rng if needed
    }
    public SafeRoom(Vector2 worldGridPosition, Vector2 shape, List<LootDrop> lootDrops, Dictionary<Direction, RoomBoundary> boundaryPoints)
    : base(worldGridPosition, shape, lootDrops, boundaryPoints)
    {
    }
    public override void Add(Room child)
    {
        if (child.ParentRoom != null)
            throw new InvalidOperationException("This room is already part of another composite!");
        _children.Add(child);
        child.ParentRoom = this;
    }

    public override void Remove(Room child)
    {
        if (_children.Remove(child))
            child.ParentRoom = null;
    }
}
