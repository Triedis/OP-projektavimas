using System.Text.Json.Serialization;
using Serilog;
using OP_Projektavimas.Utils;
class Skeleton : Enemy
{
    [JsonConstructor]
    public Skeleton() : base() { SetStrategy(new RangedStrategy()); }//runtime setint ne čia
    public Skeleton(Guid identity, Room room, Vector2 positionInRoom, Bow bow) : base(identity, room, positionInRoom, bow) { SetStrategy(new RangedStrategy()); }

    //public override ICommand? TickAI() {
    //    if (Dead) {
    //        return null;
    //    }
        
    //    Character? nearestPlayer = GetClosestOpponent();
    //    Log.Debug("Nearest player for {skeleton} is {nearestPlayer}", this, nearestPlayer);
    //    if (nearestPlayer is not null) {
    //        Vector2 direction = new(nearestPlayer.PositionInRoom.X > PositionInRoom.X ? 1 : nearestPlayer.PositionInRoom.X < PositionInRoom.X ? -1 : 0, nearestPlayer.PositionInRoom.Y > PositionInRoom.Y ? 1 : nearestPlayer.PositionInRoom.Y < PositionInRoom.Y ? -1 : 0);
    //        Vector2 newPosition = PositionInRoom + direction;

    //        MoveCommand moveCommand = new(newPosition, Identity);
    //        return moveCommand;
    //    }

    //    return null;
    //}

    public override string ToString()
    {
        return $"Skeleton";
    }

}
