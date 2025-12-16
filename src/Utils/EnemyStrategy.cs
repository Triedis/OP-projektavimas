using System.Text.Json.Serialization;
using Serilog;
namespace OP_Projektavimas.Utils
{
    public interface EnemyStrategy
    {
        ICommand? TickAI(Enemy enemy);
    }
    internal class RangedStrategy : EnemyStrategy
    {
        public ICommand? TickAI(Enemy enemy)
        {
            if (enemy.Dead) return null;

            Character? nearestPlayer = enemy.GetClosestOpponent();
            // Log.Debug("Nearest player for {skeleton} is {nearestPlayer}", enemy, nearestPlayer);
            if (nearestPlayer is null) return null;

            int distance = enemy.GetDistanceTo(nearestPlayer);
            Weapon weapon = enemy.Weapon;
            Bow bow = (Bow)weapon;
            // Attack if in range
            if (weapon is Bow && distance == bow.MaxRange && !nearestPlayer.Dead)
            {
                Log.Debug("{enemy} attacks {player} with {weapon}", enemy, nearestPlayer, weapon);
                return new UseWeaponCommand(enemy.Identity);
            }
            else if (weapon is Bow && distance > bow.MaxRange && !nearestPlayer.Dead)
            {
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
            else
            {
                // Move away from the player
                Vector2 direction = new(
                    nearestPlayer.PositionInRoom.X > enemy.PositionInRoom.X ? -1 :
                    nearestPlayer.PositionInRoom.X < enemy.PositionInRoom.X ? 1 : 0,
                    nearestPlayer.PositionInRoom.Y > enemy.PositionInRoom.Y ? -1 :
                    nearestPlayer.PositionInRoom.Y < enemy.PositionInRoom.Y ? 1 : 0
                );

                Vector2 newPosition = enemy.PositionInRoom + direction;
                Log.Debug("{enemy} moves away from {player} to {newPos}", enemy, nearestPlayer, newPosition);

                return new MoveCommand(newPosition, enemy.Identity);
            }
        }

    }
    internal class MeleeStrategy : EnemyStrategy
    {
        public ICommand? TickAI(Enemy enemy)
        {
            if (enemy.Dead) return null;

            Character? nearestPlayer = enemy.GetClosestOpponent();
            // Log.Debug("Nearest player for {zombie} is {nearestPlayer}", enemy, nearestPlayer);
            if (nearestPlayer is null) return null;

            int distance = enemy.GetDistanceTo(nearestPlayer);
            Weapon weapon = enemy.Weapon;
            if (enemy.attackTick > 0) enemy.attackTick -= 1;
            // Attack if in range
            if (distance <= weapon.MaxRange && !nearestPlayer.Dead && enemy.attackTick <= 0)
            {
                enemy.attackTick = 5;
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

    internal class ShallowSplitStrategy : EnemyStrategy
    {
        public ICommand? TickAI(Enemy enemy)
        {
            if (enemy.Dead) return null;

            Character? nearestPlayer = enemy.GetClosestOpponent();
            // Log.Debug("Nearest player for {zombie} is {nearestPlayer}", enemy, nearestPlayer);
            if (nearestPlayer is null) return null;

            int distance = enemy.GetDistanceTo(nearestPlayer);
            Weapon weapon = enemy.Weapon;
            if (enemy.attackTick > 0) enemy.attackTick -= 1;

            if (!enemy.HasSplit && enemy.Health <= enemy.StartingHealth / 2)
            {
                enemy.HasSplit = true;
                Enemy clone = enemy.ShallowClone();
                clone.SetPositionInRoom(enemy.PositionInRoom + new Vector2(1, 0));

                Log.Information("=== SHALLOW CLONE ===");
                Log.Information("Original Enemy Addr: {addr1}", enemy.GetHashCode());
                Log.Information("Cloned Enemy Addr: {addr2}", clone.GetHashCode());
                Log.Information("Original Weapon Addr: {addr3}", enemy.Weapon.GetHashCode());
                Log.Information("Cloned Weapon Addr: {addr4}", clone.Weapon.GetHashCode());

                return new SpawnEnemyCommand(clone);
            }

            // Attack if in range
            if (weapon is Dagger sword && distance <= sword.MaxRange && !nearestPlayer.Dead && enemy.attackTick <= 0)
            {
                enemy.attackTick = 5;
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
            return new MoveCommand(newPosition, enemy.Identity);
        }
    }

    internal class DeepSplitStrategy : EnemyStrategy
    {
        public ICommand? TickAI(Enemy enemy)
        {
            if (enemy.Dead) return null;

            Character? nearestPlayer = enemy.GetClosestOpponent();
            // Log.Debug("Nearest player for {zombie} is {nearestPlayer}", enemy, nearestPlayer);
            if (nearestPlayer is null) return null;

            int distance = enemy.GetDistanceTo(nearestPlayer);
            Weapon weapon = enemy.Weapon;
            if (enemy.attackTick > 0) enemy.attackTick -= 1;

            if (!enemy.HasSplit && enemy.Health <= enemy.StartingHealth / 2)
            {
                enemy.HasSplit = true;
                Enemy clone = enemy.DeepClone();
                clone.SetPositionInRoom(enemy.PositionInRoom + new Vector2(1, 0));

                Log.Information("=== DEEP CLONE ===");
                Log.Information("Original Enemy Addr: {addr1}", enemy.GetHashCode());
                Log.Information("Cloned Enemy Addr: {addr2}", clone.GetHashCode());
                Log.Information("Original Weapon Addr: {addr3}", enemy.Weapon.GetHashCode());
                Log.Information("Cloned Weapon Addr: {addr4}", clone.Weapon.GetHashCode());

                return new SpawnEnemyCommand(clone);
            }

            // Attack if in range
            if (weapon is Dagger sword && distance <= sword.MaxRange && !nearestPlayer.Dead && enemy.attackTick <= 0)
            {
                enemy.attackTick = 5;
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
            return new MoveCommand(newPosition, enemy.Identity);
        }
    }

}
