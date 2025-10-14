using System.Text.Json.Serialization;

class WorldGrid(int seed)
{
    [JsonIgnore]
    public Dictionary<Vector2, Room> Rooms { get; private set; } = [];
    [JsonPropertyName("Rooms")]
    public Dictionary<string, Room> RoomsForSerialization
    {
        get
        {
            return Rooms.ToDictionary(
                pair => $"{pair.Key.X},{pair.Key.Y}",
                pair => pair.Value
            );
        }
        set
        {
            Rooms = value.ToDictionary(
                pair => Vector2.FromKeyString(pair.Key),
                pair => pair.Value
            );
        }
    }

    public int Seed = seed;
    public readonly Random random = new(seed);

    public Room GenRoom(Vector2 position)
    {
        if (Rooms.TryGetValue(position, out Room? existingRoom)) {
            Console.WriteLine("Would dupe rooms, abort!");
            return null;
        }

        Vector2 shape = new(
            (int)random.NextInt64(10, 25),
            (int)random.NextInt64(10, 25)
        );

        Dictionary<Direction, Room?> locality = new()
        {
            { Direction.NORTH, GetRoom(position + DirectionUtils.GetVectorDirection(Direction.NORTH)) },
            { Direction.EAST, GetRoom(position + DirectionUtils.GetVectorDirection(Direction.EAST)) },
            { Direction.SOUTH, GetRoom(position + DirectionUtils.GetVectorDirection(Direction.SOUTH)) },
            { Direction.WEST, GetRoom(position + DirectionUtils.GetVectorDirection(Direction.WEST)) }
        };

        Room room = new(
                    worldGridPosition: position,
                    shape
                );

        Vector2 midPoint = new(shape.X / 2, shape.Y / 2);
        Dictionary<Direction, bool> used = [];

        int quota = 0;
        foreach (Direction dir in Enum.GetValues<Direction>())
        {
            used[dir] = false;

            locality.TryGetValue(dir, out Room? adjacent);
            if (adjacent is null)
            {
                continue;
            }

            adjacent.BoundaryPoints.TryGetValue(DirectionUtils.GetOpposite(dir), out RoomBoundary? adjacentExit);
            if (adjacentExit is null)
            {
                continue;
            }

            Console.WriteLine($"Existing room at {dir} requires exit/entry combo");

            int maxX = shape.X - 1;
            int maxY = shape.Y - 1;
            int midX = shape.X / 2;
            int midY = shape.Y / 2;
            Vector2 newBoundarypoint = dir switch
            {
                Direction.NORTH => new Vector2(midX, 0),
                Direction.EAST => new Vector2(maxX, midY),
                Direction.SOUTH => new Vector2(midX, maxY),
                Direction.WEST => new Vector2(0, midY),
                _ => new Vector2(midX, midY),
            };

            room.BoundaryPoints[dir] = new(newBoundarypoint);
            quota++;
            used[dir] = true;
        }
        Console.WriteLine($"Existing locality quota added by {quota}");


        int numBoundariesToGenerate = (int)random.NextInt64(1, 5 - quota);
        for (int i = 0; i < numBoundariesToGenerate; i++)
        {
            var unusedDirections = used.Where(pair => pair.Value == false)
                .Select(pair => pair.Key)
                .ToList();
            
            if (unusedDirections.Count == 0)
            {
                break;
            }

            int randomIndex = random.Next(unusedDirections.Count);
            Direction directionToUse = unusedDirections[randomIndex];
            Console.WriteLine($"Choosing direction {directionToUse}");

            int maxX = shape.X - 1;
            int maxY = shape.Y - 1;
            int midX = shape.X / 2;
            int midY = shape.Y / 2;
            Vector2 newBoundarypoint = directionToUse switch
            {
                Direction.NORTH => new Vector2(midX, 0),
                Direction.EAST => new Vector2(maxX, midY),
                Direction.SOUTH => new Vector2(midX, maxY),
                Direction.WEST => new Vector2(0, midY),
                _ => new Vector2(midX, midY),
            };
            room.BoundaryPoints[directionToUse] = new(newBoundarypoint);
            quota++;
            used[directionToUse] = true;
        }


        Rooms.Add(position, room);

        return room;
    }

    public Room? GetRoom(Vector2 position)
    {
        return Rooms.GetValueOrDefault(position);
    }

    public IReadOnlyCollection<Room> GetAllRooms()
    {
        return Rooms.Values;
    }
}
