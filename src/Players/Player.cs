using System.Text.Json.Serialization;

class Player : Character {
    public string Username { get; set; }
    public Color Color { get; set; }

    [JsonConstructor]
    public Player() {}

    public Player(string username, Guid identity, Color color, Room room, Vector2 positionInRoom, Sword weapon)
        : base(room, positionInRoom, weapon, identity)
    {
        Username = username;
        Color = color;
    }

    public override Character? GetClosestOpponent()
    {
        Enemy? nearestEnemy = null;
        double minDistance = double.MaxValue;

        foreach (Character character in Room.Occupants) {
            if (character is Enemy enemy && character.GetType()!=typeof(PlayerEnemyAdapter)) {
                double distance = Vector2.Distance(PositionInRoom, enemy.PositionInRoom);
                if (distance < minDistance) {
                    minDistance = distance;
                    nearestEnemy = enemy;
                }
            }
        }
        return nearestEnemy;
    }


    public override string ToString()
    {
        return $"Player {Username}";
    }
}