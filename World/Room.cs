class Room
{
    Vector2 WorldGridPosition;
    Vector2 Shape;
    List<LootDrop> LootDrops = new();
    List<Character> Occupants = new();
    Dictionary<Direction, RoomBoundary> EntryPoints = new();
    Dictionary<Direction, RoomBoundary> ExitPoints = new();

    public Room(Vector2 worldGridPosition, Vector2 shape)
    {
        this.WorldGridPosition = worldGridPosition;
        this.Shape = shape;
    }
}
