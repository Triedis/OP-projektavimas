using Serilog;

class DamageCommand(Character target, int damage) : IActionCommand
{
    private Character Target { get; } = target;
    public int Damage { get; } = damage;
    private long Age { get; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public void Execute(IStateController gameState)
    {
        Log.Information("{tgt} {id} takes {dmg} damage (is at {hp} health)", Target, Target.Identity, Damage, Target.Health);
        LogEntry takeDamageLogEntry = LogEntry.ForRoom(
            $"{Target} receives {Damage} damage. Is at {Target.Health} HP",
            Target.Room
        );
        MessageLog.Instance.Add(takeDamageLogEntry);


        Target.TakeDamage(Damage);
    }

    public bool Expired()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds() - Age > 15;
    }

    public bool CanExpire()
    {
        return true;
    }

    public void Undo(IStateController gameState)
    {
        Target.Heal(Damage);
    }

    public override string? ToString()
    {
        return $"{Damage} damage";
    }
}
