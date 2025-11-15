
using Serilog;

class ApplyStatusCommand : IActionCommand
{
    private readonly Character Target;
    private readonly int TickDamage;
    private readonly int Duration;
    private BleedingStatus status;

    public ApplyStatusCommand(Character target, int tickDamage, int duration)
    {
        Target = target;
        TickDamage = tickDamage;
        Duration = duration;
    }

    public bool CanExpire()
    {
        return false;
    }

    public override bool Equals(object? obj)
    {
        return base.Equals(obj);
    }

    public void Execute(IStateController gameState)
    {
        Log.Information("{tgt} is bleeding for {dur} turns ( health left {hp}/100", Target, Duration, Target.Health);
        MessageLog.Instance.Add(LogEntry.ForRoom($"{Target} starts bleeding!", Target.Room));

        // IActionCommand runs on an authoritative game state, so I'm not sure if this is even used
        if (gameState is ServerStateController server)
        {
            status = new BleedingStatus(Target, TickDamage, Duration);
            server.RegisterOngoingEffect(status);
        }

    }

    public bool Expired()
    {
        return status.RemainingTicks <= 0;
    }

    public override string? ToString()
    {
        return "bleeding";
    }

    public void Undo(IStateController gameState)
    {
        // no-op, as this is all indirect.
    }


}
