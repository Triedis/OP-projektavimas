class TreasureRoomFactory : IRoomFactory
{
    private readonly IEnemyFactory _guardianFactory; // Any kind of loot guardian.

    public TreasureRoomFactory(IEnemyFactory guardianFactory)
    {
        _guardianFactory = guardianFactory;
    }

    public RoomCreationResult CreateRoom(Vector2 position, WorldGrid world, Random rng)
    {
        var room = new TreasureRoom(position, world, rng);
        var enemies = new List<Enemy>();

        for (int i = 0; i < 2; i++)
        {
            var spawnPos = new Vector2(rng.Next(1, room.Shape.X - 1), rng.Next(1, room.Shape.Y - 1));
            enemies.Add(_guardianFactory.CreateEnemy(room, spawnPos));
        }

        // or something else... the logic doesn't shine here but is easily expandable.
        var legendaryWeapon = new Sword(Guid.NewGuid(), 2, new PhysicalDamageEffect(50), "Legendary Sword");
        room.LootDrops.Add(new WeaponLootDrop(legendaryWeapon, new Vector2(room.Shape.X / 2, room.Shape.Y / 2)));

        return new RoomCreationResult(room, enemies);
    }
}