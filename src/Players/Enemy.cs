using System.Text.Json.Serialization;

[JsonDerivedType(typeof(Skeleton), typeDiscriminator: "Skeleton")]
abstract class Enemy : Character {
    
    public Enemy() : base() {}
    protected Enemy(Guid identity, Room room, Vector2 positionInRoom, Weapon weapon)
        : base(room, positionInRoom, weapon, identity)
    { }
    public abstract ICommand? TickAI();

    public override Character? GetClosestOpponent()
    {
        Player? nearestPlayer = null;
        double minDistance = double.MaxValue;

        foreach (Character character in Room.Occupants) {
            if (character is Player player) {
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