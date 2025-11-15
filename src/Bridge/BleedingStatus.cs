using Serilog;

class BleedingStatus : IStatus
{
    private readonly Character Target;
    public int RemainingTicks { get; private set; }
    private readonly int TickDamage;

    public BleedingStatus(Character target, int tickDamage, int duration)
    {
        Target = target;
        TickDamage = tickDamage;
        RemainingTicks = duration;
    }
    public bool Tick()
    {
        if (RemainingTicks <= 0 || Target.Dead) return false;

        Target.TakeDamage(TickDamage);
        RemainingTicks--;
        Log.Information("{tgt} bleeds for {dmg} damage ( health {hp}/100)", Target, TickDamage, Target.Health);
        MessageLog.Instance.Add(LogEntry.ForRoom($"{Target} bleeds for {TickDamage} damage!", Target.Room));

        return RemainingTicks > 0;
    }
}