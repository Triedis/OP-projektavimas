using Serilog;

class DamageCommand(Character target, int damage) : IActionCommand
{
    private Character Target { get; } = target;
    private int Damage { get; } = damage;

    public void Execute(IStateController gameState)
    {
        Log.Information("{tgt} {id} takes {dmg} damage (is at {hp} health)", Target, Target.Identity, Damage, Target.Health);
        Target.TakeDamage(Damage);
    }

    public void Undo(IStateController gameState)
    {
        throw new NotImplementedException();
    }
}