using System.Text.Json.Serialization;

[JsonDerivedType(typeof(Skeleton), typeDiscriminator: "Skeleton")]
[JsonDerivedType(typeof(Zombie), typeDiscriminator: "Zombie")]
[JsonDerivedType(typeof(Orc), typeDiscriminator: "Orc")]
abstract class Enemy : Character {
    [JsonIgnore]
    public int AttackTick { get; set; } = 5;
    public Enemy() : base() {}
    public Enemy(Guid identity, Room room, Vector2 positionInRoom, Weapon weapon)
        : base(room, positionInRoom, weapon, identity)
    { }
    public abstract ICommand? TickAI();

    public override Character? GetClosestOpponent()
    {
        Player? nearestPlayer = null;
        double minDistance = double.MaxValue;

        foreach (Character character in Room.Occupants) {
            if (character is Player player && !character.Dead) {
                double distance = Vector2.Distance(PositionInRoom, player.PositionInRoom);
                if (distance < minDistance) {
                    minDistance = distance;
                    nearestPlayer = player;
                }
            }
        }
        return nearestPlayer;
    }

}