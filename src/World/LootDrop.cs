class LootDrop(Weapon item, Vector2 positionInRoom)
{
    public Weapon Item { get; private set; } = item;
    public Vector2 PositionInRoom { get; private set; } = positionInRoom;
}
