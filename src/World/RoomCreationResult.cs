// DTO
class RoomCreationResult
{
    public Room Room { get; }
    public List<Enemy> GeneratedEnemies { get; }

    public RoomCreationResult(Room room, List<Enemy> enemies)
    {
        Room = room;
        GeneratedEnemies = enemies;
    }
}