class StandardRoomFactory : IRoomFactory
{
    private readonly IEnemyFactory _skeletonFactory;

    public StandardRoomFactory(IEnemyFactory skeletonFactory)
    {
        _skeletonFactory = skeletonFactory;
    }


    public RoomCreationResult CreateRoom(Vector2 position, WorldGrid world, Random rng)
    {
        var room = new StandardRoom(position, world, rng);
        var enemies = new List<Enemy>();

        if (rng.NextDouble() < 0.5)
        {
            int enemyCount = rng.Next(1, 3);
            for (int i = 0; i < enemyCount; i++)
            {
                var spawnPos = new Vector2(rng.Next(1, room.Shape.X - 1), rng.Next(1, room.Shape.Y - 1));
                var factory = _skeletonFactory;
                enemies.Add(factory.CreateEnemy(room, spawnPos));
            }
        }
        return new RoomCreationResult(room, enemies);
    }
}
