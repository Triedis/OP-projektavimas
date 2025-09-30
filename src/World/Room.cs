using System.Text.Json.Serialization;

class Room(Vector2 worldGridPosition, Vector2 shape)
{
    public Vector2 WorldGridPosition = worldGridPosition;
    public Vector2 Shape = shape;
    public List<LootDrop> LootDrops = [];
    [JsonIgnore] // fucked up
    public List<Character> Occupants = [];
    public Dictionary<Direction, RoomBoundary> EntryPoints = new();
    public Dictionary<Direction, RoomBoundary> ExitPoints = new();

    public void Enter(Character character)
    {
        character.EnterRoom(this);
        Occupants.Add(character);
    }

    public void Exit(Character character, Room other)
    {
        character.EnterRoom(other);
        Occupants.Remove(character);
    }
}
