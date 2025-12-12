using System.Text.Json.Serialization;

class StandardRoom : SafeRoom
{
    public string EncounterType { get; private set; }

    // Structural initialization
    public StandardRoom(Vector2 worldGridPosition, WorldGrid world, Random rng) 
        : base(worldGridPosition, world)
    {
        EncounterType = "Common";
        Shape = new Vector2(rng.Next(10, 25), rng.Next(10, 25));
        InitializeBoundaries(world, minExits: 2, maxExits: 4); 
    }

    [JsonConstructor]
    public StandardRoom(Vector2 worldGridPosition, Vector2 shape, List<LootDrop> lootDrops, Dictionary<Direction, RoomBoundary> boundaryPoints, string encounterType)
        : base(worldGridPosition, shape, lootDrops, boundaryPoints) // Pass base properties up
    {
        EncounterType = encounterType;
    }
}
