using System.Text.Json.Serialization;
using Serilog;
namespace OP_Projektavimas.Utils
{
    interface IStrategy
    {
        ICommand? TickAI(Enemy enemy);
    }
    internal class SkeletonStrategy : IStrategy
    {
        public ICommand? TickAI(Enemy enemy)
        {
            if (enemy.Dead) return null;

            Character? nearestPlayer = enemy.GetClosestOpponent();
            if (nearestPlayer is null) return null;

            int distance = enemy.GetDistanceTo(nearestPlayer);
            Weapon weapon = enemy.Weapon;

            // Attack if in range
            if (weapon is Bow bow && distance <= bow.MaxRange && !nearestPlayer.Dead)
            {
                Log.Debug("{enemy} attacks {player} with {weapon}", enemy, nearestPlayer, weapon);
                return new UseWeaponCommand(enemy.Identity);
            }

            // Otherwise, move toward player
            Vector2 direction = new(
                nearestPlayer.PositionInRoom.X > enemy.PositionInRoom.X ? 1 :
                nearestPlayer.PositionInRoom.X < enemy.PositionInRoom.X ? -1 : 0,
                nearestPlayer.PositionInRoom.Y > enemy.PositionInRoom.Y ? 1 :
                nearestPlayer.PositionInRoom.Y < enemy.PositionInRoom.Y ? -1 : 0
            );

            Vector2 newPosition = enemy.PositionInRoom + direction;
            Log.Debug("{enemy} moves toward {player} to {newPos}", enemy, nearestPlayer, newPosition);

            return new MoveCommand(newPosition, enemy.Identity);
        }

    }
}
