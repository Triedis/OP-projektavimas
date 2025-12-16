
public class PlayerMemento
{
    internal int Health { get; }
    internal Vector2 PositionInRoom { get; }
    internal Room Room { get; }

    internal PlayerMemento(int health, Vector2 positionInRoom, Room room)
    {
        Health = health;
        PositionInRoom = positionInRoom;
        Room = room;
    }
}
