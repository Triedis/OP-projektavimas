
using Serilog;

class Orc : Enemy
{
    private readonly double locateDistance = 3;
    
    public Orc() { }
    public Orc(Guid identity, Room room, Vector2 positionInRoom, Axe weapon) : base(identity, room, positionInRoom, weapon)
    {
    }
    /// <summary>
    /// waits for player to get within certain distance to move
    /// </summary>
    /// <returns></returns>
    public override ICommand? TickAI()
    {
        if (Dead) {
            return null;
        }
        Character? nearestPlayer = GetClosestOpponent();
        Log.Debug("Nearest player for {orc} is {nearestPlayer}", this, nearestPlayer);
        if (nearestPlayer is not null) {
            int distance = base.GetDistanceTo(nearestPlayer);
            if (distance <= locateDistance)
            {
                if (base.AttackTick > 0) base.AttackTick -= 1;

                if (AttackTick <= 0 && distance <= base.Weapon.MaxRange)
                {
                    base.AttackTick = 5;
                    return new UseWeaponCommand(base.Identity);
                }
                Vector2 direction = new(nearestPlayer.PositionInRoom.X > PositionInRoom.X ? 1 : nearestPlayer.PositionInRoom.X < PositionInRoom.X ? -1 : 0, nearestPlayer.PositionInRoom.Y > PositionInRoom.Y ? 1 : nearestPlayer.PositionInRoom.Y < PositionInRoom.Y ? -1 : 0);
                Vector2 newPosition = PositionInRoom + direction;

                MoveCommand moveCommand = new(newPosition, this);
                return moveCommand;
            }

        }

        return null;
    }

    public override string ToString()
    {
        return $"Orc";
    }
}
