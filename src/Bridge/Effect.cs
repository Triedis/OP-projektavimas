using System.Text.Json.Serialization;

[JsonDerivedType(typeof(PhysicalDamageEffect), typeDiscriminator: "Physical")]
[JsonDerivedType(typeof(BleedingEffect), typeDiscriminator: "Bleeding")]
public abstract class Effect
{
    public int Power { get; set; }

    protected Effect(int power)
    {
        Power = power;
    }

    /// <summary>
    /// Applies effect of weapon
    /// </summary>
    /// <param name="actor">Weapon wielder</param>
    /// <param name="target">Target character</param>
    /// <returns>List of actions to apply to the game state</returns>
    public abstract IReadOnlyList<IActionCommand> Apply(Character actor, Character target);
}