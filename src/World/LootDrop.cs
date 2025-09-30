class LootDrop
{
    Sword Item { get; }
    Vector2 PositionInRoom { get; }

    public LootDrop(Sword item, Vector2 positionInRoom)
    {
        this.Item = item;
        this.PositionInRoom = positionInRoom;
    }
}
