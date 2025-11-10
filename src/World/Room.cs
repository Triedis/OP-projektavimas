using System.Text.Json.Serialization;
using Serilog;

[JsonDerivedType(typeof(StandardRoom), typeDiscriminator: "StandardRoom")]
[JsonDerivedType(typeof(TreasureRoom), typeDiscriminator: "TreasureRoom")]
[JsonDerivedType(typeof(BossRoom), typeDiscriminator: "BossRoom")]
abstract class Room
{
    public Vector2 WorldGridPosition { get; }
    public Vector2 Shape { get; protected set; }
    public List<LootDrop> LootDrops { get; protected set; } = [];
    
    [JsonIgnore]
    public List<Character> Occupants { get; private set; } = [];
    public Dictionary<Direction, RoomBoundary> BoundaryPoints { get; protected set; } = [];

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

    
    // Common logic
    public virtual void Enter(Character character)
    {
        character.Room.Exit(character);
        character.EnterRoom(this);
        Occupants.Add(character);
    }

    public void Enter(Character character, Direction enteringFrom) {
        Enter(character);

        RoomBoundary? entryPoint = BoundaryPoints.GetValueOrDefault(enteringFrom!);
        if (entryPoint is null)
        {
            Log.Error("Entering from nothing? Logic bug guard triggered");
            throw new Exception();
        }

        character.SetPositionInRoom(entryPoint.PositionInRoom
            + DirectionUtils.GetVectorDirection(
                enteringFrom!
            ).ToScreenSpace());
    }

    public virtual void Exit(Character character)
    {
        Occupants.Remove(character);
    }
    
    public void Exit(Character character, Room other)
    {
        character.EnterRoom(other);
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