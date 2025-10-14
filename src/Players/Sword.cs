class Sword : Weapon
{
    public int MaxRange { get; set; }
    public int Damage { get; set; }

    public Sword(int maxRange, int damage) : base() {
        MaxRange = maxRange;
        Damage = damage;
    }

    public override IReadOnlyList<IActionCommand> Act(Character actor, Character target)
    {
        var results = new List<IActionCommand>
        {
            new DamageCommand(target, Damage)
        };
        return results;
    }

    public override bool CanUse(Character actor, Character target, IStateController gameState)
    {
        if (actor.GetDistanceTo(target) > MaxRange)
        {
            return false;
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode(); // TODO
    }

    public override string? ToString()
    {
        return base.ToString(); // TODO
    }
}