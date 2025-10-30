
class BleedingEffect : Effect
{
    public int Duration { get; set; }
    private readonly Random rng = new();
    public BleedingEffect(int power, int duration) : base(power)
    {
        this.Duration = duration;
    }

    public override IReadOnlyList<IActionCommand> Apply(Character actor, Character target)
    {
        var actions = new List<IActionCommand>
        {
            new DamageCommand(target, Power)
        };
        if (rng.Next(2) == 1)
            actions.Add(new ApplyBleedCommand(target, Power, Duration));

        return actions;
    }
}