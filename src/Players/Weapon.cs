// Abstract weapon class.
using System.Text.Json.Serialization;

[JsonDerivedType(typeof(Sword), typeDiscriminator: "Sword")]
abstract class Weapon()
{
    /// <summary>
    /// Determines whether the game state situation allows for the weapon to be used.
    /// This is left up for implementation because for example while a sword is short-range, magic may not be.
    /// </summary>
    /// <param name="actor">Weapon wielder</param>
    /// <param name="target">Target character</param>
    /// <param name="gameState">Current authoritative gamestate</param>
    /// <returns>Boolean indicating whether the weapon can be used on target</returns>
    public abstract bool CanUse(Character actor, Character target, IStateController gameState);
    /// <summary>
    /// Applies one or more actions on the target (or even also on the actor) based on the weapon's characteristics.
    /// For example, a sword will likely only cause damage and/or bleed, but certain spellbooks may heal.
    /// </summary>
    /// <param name="actor">Weapon wielder</param>
    /// <param name="target">Target character</param>
    /// <returns>List of actions to apply to the game state</returns>
    public abstract IReadOnlyList<IActionCommand> Act(Character actor, Character target);
}
