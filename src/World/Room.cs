using System.Text.Json.Serialization;

class Room(Vector2 worldGridPosition, Vector2 shape)
{
    public Vector2 WorldGridPosition = worldGridPosition;
    public Vector2 Shape = shape;
    public List<LootDrop> LootDrops = [];
    [JsonIgnore] // resolve infinite-recursion serialization bug
    public List<Character> Occupants { get; private set; } = [];
    public Dictionary<Direction, RoomBoundary> BoundaryPoints = [];

    public IEnumerable<Character> GetCharacters() {
        return Occupants;
    }

    public void Enter(Character character)
    {
        character.Room.Exit(character);
        character.EnterRoom(this);
        Occupants.Add(character);
    }

    public void Exit(Character character)
    {
        Occupants.Remove(character);
    }

    public void Enter(Character character, Direction enteringFrom) {
        Enter(character);

        RoomBoundary? entryPoint = BoundaryPoints.GetValueOrDefault(enteringFrom!);
        if (entryPoint is null)
        {
            Console.WriteLine("Entering from nothing? Logic bug guard triggered");
            throw new Exception();
        }

        character.SetPositionInRoom(entryPoint.PositionInRoom
            + DirectionUtils.GetVectorDirection(
                enteringFrom!
            ).ToScreenSpace());
    }

    public override bool Equals(object? obj)
    {
        return obj is Room room && room.WorldGridPosition.Equals(WorldGridPosition);
    }

    public void Exit(Character character, Room other)
    {
        character.EnterRoom(other);
        Occupants.Remove(character);
    }

    public override int GetHashCode()
    {
        return WorldGridPosition.GetHashCode();
    }

    public override string? ToString()
    {
        return WorldGridPosition.ToString();
    }
}
