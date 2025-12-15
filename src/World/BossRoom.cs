
public class BossRoom : SafeRoom
{
    public Character Boss { get; private set; }
    public bool IsBossDefeated { get; set; } = false;

    public BossRoom(Vector2 worldGridPosition, WorldGrid world) : base(worldGridPosition, world)
    {
                Shape = new Vector2(30, 30);
                InitializeSingleEntrance(world);
                
                Boss = new OrcFactory().CreateEnemy(this, new Vector2(Shape.X / 2, Shape.Y / 2));
                Occupants.Add(Boss);
            }
    private void InitializeSingleEntrance(WorldGrid world)
    {
        var directions = Enum.GetValues<Direction>().ToList();
        var shuffledDirections = directions.OrderBy(x => Random.Shared.Next()).ToList();

        foreach (Direction dir in shuffledDirections)
        {
            Vector2 adjacentPos = WorldGridPosition + DirectionUtils.GetVectorDirection(dir);
            Room? adjacentRoom = world.GetRoom(adjacentPos);

            if (adjacentRoom != null && adjacentRoom.BoundaryPoints.ContainsKey(DirectionUtils.GetOpposite(dir)))
            {
                // Found an adjacent room to connect to.
                BoundaryPoints[dir] = CreateBoundaryForDirection(dir);
                return; // Stop after creating one entrance.
            }
        }

        // Fallback if no adjacent room is connectable (e.g., first room generated)
        if (BoundaryPoints.Count == 0)
        {
            BoundaryPoints[Direction.SOUTH] = CreateBoundaryForDirection(Direction.SOUTH);
        }
    }
    public override void Accept(IRoomVisitor visitor)
    {
        visitor.Visit(this);
    }
}

