
using System.Text.Json.Serialization;
using Serilog;

class VampiricSword : Weapon
{
    public float LifestealPercentage { get; private set; }

    public VampiricSword(Guid identity, int maxRange, Effect effect, float lifestealPercentage = 0.5f)
        : base(identity, maxRange, effect)
    {
        LifestealPercentage = lifestealPercentage;
    }

    // [JsonConstructor]
    // public VampiricSword(Guid identity, int maxRange, Effect effect, float lifestealPercentage)
    //     : base(identity, maxRange, effect) // Pass base properties up
    // {
    //     LifestealPercentage = lifestealPercentage;
    // }

    public override IReadOnlyList<IActionCommand> Act(Character actor, Character target)
    {
        var commands = new List<IActionCommand>();

        IReadOnlyList<IActionCommand> damageCommands = Effect.Apply(actor, target);
        commands.AddRange(damageCommands);
        
        int totalDamage = 0;
        foreach (var command in damageCommands)
        {
            if (command is DamageCommand damageCommand)
            {
                totalDamage += damageCommand.Damage;
            }
        }

        if (totalDamage > 0)
        {
            int healAmount = (int)Math.Ceiling(totalDamage * LifestealPercentage);
            if (healAmount > 0)
            {
                commands.Add(new VampiricHealCommand(actor, healAmount));
            }
        }
        
        return commands;
    }

    public override bool CanUse(Character actor, Character target, IStateController gameState)
    {
        // same logic as a standard Sword
        if (actor.GetDistanceTo(target) > MaxRange || target.Dead)
        {
            return false;
        }

        return true;
    }

    public override string ToString()
    {
        return "Vampiric Sword";
    }
}

  