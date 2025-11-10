class BossRoomFactory : IRoomFactory
{
    private readonly IEnemyFactory _bossFactory = new SkeletonFactory();
    private readonly IEnemyFactory _minionFactory = new SkeletonFactory();

    public RoomCreationResult CreateRoom(Vector2 position, WorldGrid world, Random rng)
    {
        var room = new BossRoom(position, world);
        var enemies = new List<Enemy>();

        Vector2 bossPos = new(room.Shape.X / 2, room.Shape.Y / 2);
        var boss = _bossFactory.CreateEnemy(room, bossPos);
        enemies.Add(boss);

        // optional minions based on some difficulty factor, currently amount of rooms
        int minionCount = Math.Min(world.Rooms.Count / 5, 4); // 1 minion per 5 rooms, max 4
        for (int i = 0; i < minionCount; i++)
        {
            Vector2 minionPos = new(world.random.Next(1, room.Shape.X - 1), world.random.Next(1, room.Shape.Y - 1));
            enemies.Add(_minionFactory.CreateEnemy(room, minionPos));
        }
        
        return new RoomCreationResult(room, enemies);
    }
}