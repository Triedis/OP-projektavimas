using Serilog;

class VampiricHealCommand(Character actor, int healAmount) : IActionCommand
{
    private Character Actor { get; } = actor;
    private int HealAmount { get; } = healAmount;
    private long Age { get; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    public void Execute(IStateController gameState)
    {
        Log.Information("{actor} gains {heal} temporary health from lifesteal.", Actor, HealAmount);
        LogEntry gainHealthLogEntry = LogEntry.ForRoom(
            $"{Actor} feels a surge of stolen vitality (+{HealAmount} HP)!",
            Actor.Room
        );
        MessageLog.Instance.Add(gainHealthLogEntry);
        
        Actor.Heal(HealAmount);
    }

    public void Undo(IStateController gameState)
    {
        Log.Information("Temporary health on {actor} fades.", Actor);
        LogEntry loseHealthLogEntry = LogEntry.ForRoom(
            $"The stolen vitality fades from {Actor}.",
            Actor.Room
        );
        MessageLog.Instance.Add(loseHealthLogEntry);
        
        // This ensures the player cannot die from the effect ending.
        int damageToDeal = HealAmount;
        if (Actor.Health <= HealAmount)
        {
            damageToDeal = Actor.Health - 1; // min at 1 hp
        }

        if (damageToDeal > 0)
        {
            // ensured it's not a lethal amount.
            Actor.TakeDamage(damageToDeal);
        }
    }

    public bool CanExpire()
    {
        return true;
    }

    public bool Expired()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds() - Age > 10;
    }
    
    public override string? ToString()
    {
        return $"+{HealAmount} Temporary HP";
    }
}