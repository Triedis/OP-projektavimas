using System.Text.Json;
using System.Text.Json.Serialization;

// Represents a weapon that can be dropped as loot.
public class WeaponLootDrop : LootDrop
{
    public Weapon Item { get; set; }

    [JsonConstructor]
    public WeaponLootDrop(Weapon item, Vector2 positionInRoom) : base(positionInRoom)
    {
        Item = item;
    }

    public override string Collect(Player player)
    {
        // For now, let's assume the player always equips the new weapon.
        // A more complex system might add it to an inventory.
        player.Weapon = Item;
        return $"You found and equipped: {Item.Name}";
    }
}
