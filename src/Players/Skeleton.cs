using Serilog;

class Skeleton : Enemy
{
    public Skeleton() : base() {}
    public Skeleton(Guid identity, Room room, Vector2 positionInRoom, Sword sword) : base(identity, room, positionInRoom, sword) { }

    public override ICommand? TickAI() {
        if (Dead) {
            return null;
        }
        
        Character? nearestPlayer = GetClosestOpponent();
        Log.Debug("Nearest player for {skeleton} is {nearestPlayer}", this, nearestPlayer);
        if (nearestPlayer is not null) {
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
