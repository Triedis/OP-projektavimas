abstract class Character
{
    Vector2 PositionInRoom { get; }
    int Health { get; set; }
    Sword Weapon { get; }
    Boolean Dead { get; }

    public Character(Vector2 positionInRoom, Sword weapon)
    {
        this.PositionInRoom = positionInRoom;
        this.Weapon = weapon;
    }
}
