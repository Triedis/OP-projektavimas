using System.Text.Json.Serialization;

// An abstract base class for different types of loot that can be dropped in a room.
[JsonDerivedType(typeof(WeaponLootDrop), typeDiscriminator: "Weapon")]
public abstract class LootDrop
{
    public Vector2 PositionInRoom { get; set; }

    [JsonConstructor]
    public LootDrop(Vector2 positionInRoom)
    {
        PositionInRoom = positionInRoom;
    }

    // The action to perform when a player collects the loot.
    // Returns a string describing what was collected.
    public abstract string Collect(Player player);
}
