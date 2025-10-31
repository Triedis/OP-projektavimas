
using Serilog;

class ApplyStatusCommand : IActionCommand
{
    private readonly Character Target;
    private readonly int TickDamage;
    private readonly int Duration;
    public ApplyStatusCommand(Character target, int tickDamage, int duration)
    {
        Target = target;
        TickDamage = tickDamage;
        Duration = duration;
    }
    public void Execute(IStateController gameState)
    {
        Log.Information("{tgt} is bleeding for {dur} turns ( health left {hp}/100", Target, Duration, Target.Health);
        MessageLog.Instance.Add(LogEntry.ForRoom($"{Target} starts bleeding!", Target.Room));

        if (gameState is ServerStateController server)
        {
            server.Game.RegisterOngoingEffect(new BleedingStatus(Target, TickDamage, Duration));
        }

    }

    public void Undo(IStateController gameState)
    {
        throw new NotImplementedException("Can implement undo of effects");
    }
}