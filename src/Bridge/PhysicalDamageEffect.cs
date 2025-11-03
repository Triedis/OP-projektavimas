class PhysicalDamageEffect : Effect
{
    public PhysicalDamageEffect(int power) : base(power) { }

    public override IReadOnlyList<IActionCommand> Apply(Character actor, Character target)
    {
        return new List<IActionCommand>
        {
            new DamageCommand(target, Power)
        };
    }
}
