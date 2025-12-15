public class Sword(Guid identity, int maxRange, Effect effect, string name) : Weapon(identity, maxRange, effect)
{
    public override string Name { get; } = name;

    public override IReadOnlyList<IActionCommand> Act(Character actor, Character target)
    {
        return Effect.Apply(actor, target);
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