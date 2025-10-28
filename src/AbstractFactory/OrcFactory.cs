
class OrcFactory : IEnemyFactory
{
    private const int MinRange = 1;
    private const int MaxRange = 2;
    private const int MinDamage = 15;
    private const int MaxDamage = 35;
    private readonly Random random = new();
    public Enemy CreateEnemy(Room room, Vector2 pos)
    {
        return new Orc(Guid.NewGuid(), room, pos, (Axe)CreateWeapon());
    }

    public Weapon CreateWeapon()
    {
        return new Axe(random.Next(MinRange, MaxRange), random.Next(MinDamage, MaxDamage), Guid.NewGuid());
    }
}
