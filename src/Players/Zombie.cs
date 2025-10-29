
using System.Text.Json.Serialization;
using Serilog;
using OP_Projektavimas.Utils;
class Zombie : Enemy
{
    [JsonIgnore]
    public bool SeenPlayer = false;
    public Zombie() { SetStrategy(new MeleeStrategy()); }
    public Zombie(Guid identity, Room room, Vector2 positionInRoom, Sword weapon) : base(identity, room, positionInRoom, weapon)
    {
        SetStrategy(new MeleeStrategy());
    }
    /// <summary>
    /// waits for player to get within certain distance and makes this and other zombies within the room attack 
    /// </summary>
    /// <returns></returns>
    // public ICommand? TickAI()
    // {
    //     if (Dead)
    //     {
    //         return null;
    //     }
        
    //     Character? nearestPlayer = GetClosestOpponent();
    //     Log.Debug("Nearest player for {zombie} is {nearestPlayer}", this, nearestPlayer);

    //     if (nearestPlayer is not null) {
    //         int distance = base.GetDistanceTo(nearestPlayer);
    //         if (!SeenPlayer && distance <= locateDistance)
    //         {
    //             foreach (var enemy in base.Room.GetCharacters().OfType<Zombie>())
    //             {
    //                 enemy.SeenPlayer = true;
    //             }
    //             this.SeenPlayer = true;
    //         }
    //         if(SeenPlayer)
    //         {
    //             if (base.AttackTick > 0) base.AttackTick -= 1;
                
    //             if(AttackTick <= 0 && distance <= base.Weapon.MaxRange)
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
        return $"Zombie";
    }
}
