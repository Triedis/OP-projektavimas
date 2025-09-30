class Player(string username, Color color, Room room, Vector2 positionInRoom, Sword weapon) : Character(room, positionInRoom, weapon, username)
{
    public string Username { get; } = username;
    public Color Color { get; } = color;
}
