interface IRoomFactory
{
    RoomCreationResult CreateRoom(Vector2 position, WorldGrid world, Random rng);
}