class WorldGrid
{
    private Dictionary<Vector2, Room> Rooms = new();

    public WorldGrid()
    {

    }

    public Room GenRoom(Vector2 position)
    {
        Room room = new Room(
                    worldGridPosition: position,
                    shape: new(15,20) // Randomize?
                );
        Rooms.Add(position, room);

        return room;
    }

    public Room GetRoom(Vector2 position)
    {
        return Rooms[position];
    }

    public IReadOnlyCollection<Room> GetAllRooms() {
        return Rooms.Values;
    }
}
