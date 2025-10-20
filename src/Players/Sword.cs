class Sword(int maxRange, int damage, Guid identity) : Weapon(identity)
{
    public int MaxRange { get; set; } = maxRange;
    public int Damage { get; set; } = damage;

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

        if (target.Dead) {
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