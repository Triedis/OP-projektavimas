class Axe : Weapon
{
    public Axe(Guid identity, int maxRange, Effect effect) : base(identity, maxRange, effect){}

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

        return true;
    }
    public override string ToString()
    {
        return "Axe";
    }
}
