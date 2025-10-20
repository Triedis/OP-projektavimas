using Serilog;

class Skeleton : Enemy
{
    public Skeleton() : base() {}
    public Skeleton(Guid identity, Room room, Vector2 positionInRoom, Dagger weapon) : base(identity, room, positionInRoom, weapon){  }

    public override ICommand? TickAI() {
        if (Dead) {
            return null;
        }
        
        Character? nearestPlayer = GetClosestOpponent();
        Log.Debug("Nearest player for {skeleton} is {nearestPlayer}", this, nearestPlayer);
        if (nearestPlayer is not null) {
            if (base.AttackTick > 0) base.AttackTick -= 1;

            if (AttackTick <= 0 && base.GetDistanceTo(nearestPlayer) <= base.Weapon.MaxRange)
            {
                base.AttackTick = 5;
                return new UseWeaponCommand(base.Identity);
            }
            Vector2 direction = new(nearestPlayer.PositionInRoom.X > PositionInRoom.X ? 1 : nearestPlayer.PositionInRoom.X < PositionInRoom.X ? -1 : 0, nearestPlayer.PositionInRoom.Y > PositionInRoom.Y ? 1 : nearestPlayer.PositionInRoom.Y < PositionInRoom.Y ? -1 : 0);
            Vector2 newPosition = PositionInRoom + direction;

            MoveCommand moveCommand = new(newPosition, this);
            return moveCommand;
        }

        return null;
    }

    public override string ToString()
    {
        return $"Skeleton";
    }

}
