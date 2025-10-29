
using Serilog;
using OP_Projektavimas.Utils;
class Orc : Enemy
{
    
    public Orc() { SetStrategy(new MeleeStrategy()); }
    public Orc(Guid identity, Room room, Vector2 positionInRoom, Axe weapon) : base(identity, room, positionInRoom, weapon)
    {
        SetStrategy(new MeleeStrategy());
    }
    /// <summary>
    /// waits for player to get within certain distance to move
    /// </summary>
    /// <returns></returns>
    // public ICommand? TickAI()
    // {
    //     if (Dead) {
    //         return null;
    //     }
    //     Character? nearestPlayer = GetClosestOpponent();
    //     Log.Debug("Nearest player for {orc} is {nearestPlayer}", this, nearestPlayer);
    //     if (nearestPlayer is not null) {
    //         int distance = base.GetDistanceTo(nearestPlayer);
    //         if (distance <= locateDistance)
    //         {
    //             if (base.AttackTick > 0) base.AttackTick -= 1;

    //             if (AttackTick <= 0 && distance <= base.Weapon.MaxRange)
    //             {
    //                 base.AttackTick = 5;
    //                 return new UseWeaponCommand(base.Identity);
    //             }
    //             Vector2 direction = new(nearestPlayer.PositionInRoom.X > PositionInRoom.X ? 1 : nearestPlayer.PositionInRoom.X < PositionInRoom.X ? -1 : 0, nearestPlayer.PositionInRoom.Y > PositionInRoom.Y ? 1 : nearestPlayer.PositionInRoom.Y < PositionInRoom.Y ? -1 : 0);
    //             Vector2 newPosition = PositionInRoom + direction;

    //             MoveCommand moveCommand = new(newPosition, base.Identity);
    //             return moveCommand;
    //         }

    //     }

    //     return null;
    // }

    public override string ToString()
    {
        return $"Orc";
    }
}
