

class Axe : Weapon
{
    public Axe(int maxRange, int damage, Guid identity) : base(identity, maxRange, damage){}

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
    public override string ToString()
    {
        return "Axe";
    }
}
