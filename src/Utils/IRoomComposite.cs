interface IRoomComposite
{
    void Add(Room room);
    void Remove(Room room);
    IReadOnlyList<Room> GetChildren();
}
