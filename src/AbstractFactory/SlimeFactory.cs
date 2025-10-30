class SlimeFactory : IEnemyFactory
{
    private const int MinDamage = 1;
    private const int MaxDamage = 4;
    private const int MinDuration = 1;
    private const int MaxDuration = 6;
    private readonly Random random = new();
    public Enemy CreateEnemy(Room room, Vector2 pos)
    {
        return new Slime(Guid.NewGuid(), room, pos, (Dagger)CreateWeapon());
    }

    public Weapon CreateWeapon()
    {
        return new Dagger(Guid.NewGuid(), 1, new BleedingEffect(random.Next(MinDamage, MaxDamage), random.Next(MinDuration, MaxDuration)));
    }
}