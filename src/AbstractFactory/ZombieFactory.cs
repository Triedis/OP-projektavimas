


class ZombieFactory : IEnemyFactory
{

    private const int MinRange = 1;
    private const int MaxRange = 2;
    private const int MinDamage = 10;
    private const int MaxDamage = 25;
    private readonly Random random = new();
    public Enemy CreateEnemy(Room room, Vector2 pos)
    {
        return new Zombie(Guid.NewGuid(), room, pos, (Sword)CreateWeapon());
    }

    public Weapon CreateWeapon()
    {
        return new Sword(random.Next(MinRange, MaxRange), random.Next(MinDamage, MaxDamage), Guid.NewGuid());
    }
}
