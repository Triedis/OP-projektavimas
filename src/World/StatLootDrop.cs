using System.Text.Json.Serialization;

// Represents a loot drop that increases player stats (Health).
public class StatLootDrop : LootDrop
{
    public int HealthBoost { get; set; }

    [JsonConstructor]
    public StatLootDrop(int healthBoost, Vector2 positionInRoom) : base(positionInRoom)
    {
        HealthBoost = healthBoost;
    }

    public override string Collect(Player player)
    {
        player.StartingHealth += HealthBoost;
        player.Heal(HealthBoost);
        return $"You found a Health Potion! Max Health increased by {HealthBoost}. HP restored.";
    }
}
