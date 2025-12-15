// A visitor that counts the number of enemies in each type of room.
public class EnemyCountVisitor : IRoomVisitor
{
    public int StandardRoomEnemies { get; private set; }
    public int TreasureRoomEnemies { get; private set; }
    public int BossRoomEnemies { get; private set; }
    public int TotalEnemies => StandardRoomEnemies + TreasureRoomEnemies + BossRoomEnemies;

    public void Visit(StandardRoom room)
    {
        StandardRoomEnemies += room.Occupants.OfType<Enemy>().Count(e => !e.Dead);
    }

    public void Visit(TreasureRoom room)
    {
        TreasureRoomEnemies += room.Occupants.OfType<Enemy>().Count(e => !e.Dead);
    }

    public void Visit(BossRoom room)
    {
        BossRoomEnemies += room.Occupants.OfType<Enemy>().Count(e => !e.Dead);
    }

    public string GetReport()
    {
        return $"Enemy Report: Standard Rooms: {StandardRoomEnemies}, Treasure Rooms: {TreasureRoomEnemies}, Boss Rooms: {BossRoomEnemies}, Total: {TotalEnemies}";
    }
}