using DungeonCrawler.src.Iterators;
using System.Text.Json.Serialization;

public class WorldGrid(int seed):IterableCollection<Room>
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
    public void PrintRoomTree(Room root)
    {
        PrintRoomRecursive(root, 0);
    }

    public Iterator<Room> CreateIterator()
    {
        return new WorldGridIterator(Rooms.Values.ToList());
    }

    public void ClearAllOccupants()
    {
        foreach (var room in Rooms.Values)
        {
            room.Occupants.Clear();
        }
    }

    private void PrintRoomRecursive(Room room, int indent)
    {
        Console.WriteLine($"{new string(' ', indent * 2)}- {room.GetType().Name} at {room.WorldGridPosition}");

        foreach (var child in room.GetChildren())
            PrintRoomRecursive(child, indent + 1);
    }
    /// <summary>
    /// Generates a new room at the specified position.
    /// This method now uses the Factory pattern to create different types of rooms,
    /// adding variety and extensibility to the dungeon generation process.
    /// </summary>
    /// <param name="position">The grid position to generate the room at.</param>
    /// <returns>The newly created Room, or null if a room already exists at that position.</returns>
    // public Room? GenRoom(Vector2 position)
    // {
    //     if (Rooms.ContainsKey(position))
    //     {
    //         Console.WriteLine("Would dupe rooms, abort!");
    //         return null;
    //     }

    //     IRoomFactory selectedFactory;
    //     double chance = random.NextDouble();

    //     // Boss rooms are rare and should only appear after the dungeon has some size.
    //     if (chance < 0.05 && Rooms.Count > 5)
    //     {
    //         selectedFactory = bossRoomFactory;
    //     }
    //     // Treasure rooms are uncommon.
    //     else if (chance < 0.20)
    //     {
    //         selectedFactory = treasureRoomFactory;
    //     }
    //     // Standard rooms are the most common.
    //     else
    //     {
    //         selectedFactory = standardRoomFactory;
    //     }

    //     // Delegate the room creation to the selected factory.
    //     Room room = selectedFactory.CreateRoom(position, this);
    //     Rooms.Add(position, room);


    //     return room;
    // }

    public Room? GetRoom(Vector2 position)
    {
        return Rooms.GetValueOrDefault(position);
    }

    public IReadOnlyCollection<Room> GetAllRooms()
    {
        return Rooms.Values;
    }

    public void Accept(IRoomVisitor visitor)
    {
        foreach (var room in Rooms.Values)
        {
            room.Accept(visitor);
        }
    }
}
