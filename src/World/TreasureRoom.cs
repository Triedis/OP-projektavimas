using System.Text.Json.Serialization;

class TreasureRoom : Room
{
    private readonly IEnemyFactory _orcFactory = new OrcFactory();
    public bool IsSealed { get; private set; }

    public TreasureRoom(Vector2 worldGridPosition, WorldGrid world, Random rng) : base(worldGridPosition, world)
    {
        IsSealed = false;
        Shape = new Vector2(
            world.random.Next(8, 15),
            world.random.Next(8, 15)
        );

        InitializeBoundaries(world, minExits: 1, maxExits: 2);
    }

    public override void Enter(Character character)
    {
        base.Enter(character);

        if (character is Enemy enemy) { enemy.OnDeath += OnOccupantDeath; }

        if (character is Player && !IsSealed && Occupants.OfType<Enemy>().Any(e => !e.Dead))
        {
            SealRoom();
        }
    }

    [JsonConstructor]
    public TreasureRoom(Vector2 worldGridPosition, Vector2 shape, List<LootDrop> lootDrops, Dictionary<Direction, RoomBoundary> boundaryPoints, bool isSealed)
        : base(worldGridPosition, shape, lootDrops, boundaryPoints)
    {
        IsSealed = isSealed;
    }

    private void OnOccupantDeath(Character deceased)
    {
        deceased.OnDeath -= OnOccupantDeath; // unbind
        CheckForUnseal();
    }

    public void SealRoom()
    {
        if (Occupants.Any(c => c is Enemy))
        {
            IsSealed = true;
            LogEntry slamShutMessage = LogEntry.ForRoom(
                "The door slams shut! You must defeat the loot guards!",
                this
            );
            MessageLog.Instance.Add(slamShutMessage);
        }
    }

    private void CheckForUnseal()
    {
        if (IsSealed && !Occupants.OfType<Enemy>().Any(e => !e.Dead))
        {
            IsSealed = false;
            MessageLog.Instance.Add(LogEntry.ForRoom("With the last guardian defeated, the door unseals.", this));
        }
    }

    public void UnsealRoom()
    {
        IsSealed = false;
        LogEntry slamShutMessage = LogEntry.ForRoom(
            "The way is clear, you may exit.",
            this
        );
        MessageLog.Instance.Add(slamShutMessage);
    }
}