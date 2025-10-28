


class SkeletonFactory : IEnemyFactory
{
    private const int MinRange = 1;
    private const int MaxRange = 1;
    private const int MinDamage = 5;
    private const int MaxDamage = 15;
    private readonly Random random = new();
    public Enemy CreateEnemy(Room room, Vector2 pos)
    {
        return new Skeleton(Guid.NewGuid(), room, pos, (Bow)CreateWeapon());
    }

    public Weapon CreateWeapon()
    {
        return new Bow(random.Next(MinRange, MaxRange), random.Next(MinDamage, MaxDamage), Guid.NewGuid());
    }
}
