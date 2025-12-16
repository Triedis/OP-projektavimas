using System.Text.Json.Serialization;
using Serilog;
namespace OP_Projektavimas.Utils
{
    public abstract class EnemyStrategy
    {
        // TEMPLATE METHOD
        public ICommand? TickAI(Enemy enemy)
        {
            if (enemy.Dead) return null;

            Character? nearestPlayer = enemy.GetClosestOpponent();
            if (nearestPlayer is null) return null;

            Log.Debug("Nearest player for {enemy} is {nearestPlayer}", enemy, nearestPlayer);

            if (enemy.attackTick > 0)
                enemy.attackTick--;

            ICommand? special = TrySpecialAction(enemy, nearestPlayer);
            if (special != null) return special;

            if (TryAttack(enemy, nearestPlayer, out ICommand? attackCmd))
                return attackCmd;

            
            return Move(enemy, nearestPlayer);
        }

        protected virtual ICommand? TrySpecialAction(Enemy enemy, Character player) => null;


        protected abstract bool TryAttack(
            Enemy enemy,
            Character player,
            out ICommand? command
        );

        protected virtual ICommand Move(Enemy enemy, Character player)
        {
            Vector2 direction = new(
                player.PositionInRoom.X > enemy.PositionInRoom.X ? 1 :
                player.PositionInRoom.X < enemy.PositionInRoom.X ? -1 : 0,
                player.PositionInRoom.Y > enemy.PositionInRoom.Y ? 1 :
                player.PositionInRoom.Y < enemy.PositionInRoom.Y ? -1 : 0
            );

            Vector2 newPosition = enemy.PositionInRoom + direction;

            Log.Debug("{enemy} moves toward {player} to {newPos}",
                enemy, player, newPosition);

            return new MoveCommand(newPosition, enemy.Identity);
        }
    }
    internal sealed class RangedStrategy : EnemyStrategy
    {
        protected override bool TryAttack(
            Enemy enemy,
            Character player,
            out ICommand? command)
        {
            command = null;

            Weapon weapon = enemy.Weapon;
            if (weapon is not Bow bow) return false;

            int distance = enemy.GetDistanceTo(player);

            if (!player.Dead && distance == bow.MaxRange)
            {
                Log.Debug("{enemy} attacks {player} with {weapon}",
                    enemy, player, weapon);

                command = new UseWeaponCommand(enemy.Identity);
                return true;
            }

            return false;
        }

        protected override ICommand Move(Enemy enemy, Character player)
        {
            Weapon weapon = enemy.Weapon;
            Bow bow = (Bow)weapon;

            int distance = enemy.GetDistanceTo(player);

            // Move AWAY if too close
            if (distance < bow.MaxRange)
            {
                Vector2 direction = new(
                    player.PositionInRoom.X > enemy.PositionInRoom.X ? -1 :
                    player.PositionInRoom.X < enemy.PositionInRoom.X ? 1 : 0,
                    player.PositionInRoom.Y > enemy.PositionInRoom.Y ? -1 :
                    player.PositionInRoom.Y < enemy.PositionInRoom.Y ? 1 : 0
                );

                Vector2 newPosition = enemy.PositionInRoom + direction;

                Log.Debug("{enemy} moves away from {player} to {newPos}",
                    enemy, player, newPosition);

                return new MoveCommand(newPosition, enemy.Identity);
            }

            return base.Move(enemy, player);
        }

    }
    internal sealed class MeleeStrategy : EnemyStrategy
    {
        protected override bool TryAttack(
            Enemy enemy,
            Character player,
            out ICommand? command)
        {

            if (enemy.Dead)
            {
              command = null;
              return null;
             }

            Character? nearestPlayer = enemy.GetClosestOpponent();
            // Log.Debug("Nearest player for {zombie} is {nearestPlayer}", enemy, nearestPlayer);
            if (nearestPlayer is null)
            {
              command = null;
              return null;
            }

            int distance = enemy.GetDistanceTo(player);
            Weapon weapon = enemy.Weapon;

            if (!player.Dead && distance <= weapon.MaxRange && enemy.attackTick <= 0)
            {
                enemy.attackTick = 5;

                Log.Debug("{enemy} attacks {player} with {weapon}",
                    enemy, player, weapon);

                command = new UseWeaponCommand(enemy.Identity);
                return true;
            }

            return false;
        }

    }

    internal sealed class ShallowSplitStrategy : EnemyStrategy
    {
        protected override ICommand? TrySpecialAction(Enemy enemy, Character player)
        {

            if (enemy.Dead)
            {
              command = null;
              return null;
             }

            Character? nearestPlayer = enemy.GetClosestOpponent();
            // Log.Debug("Nearest player for {zombie} is {nearestPlayer}", enemy, nearestPlayer);
            if (nearestPlayer is null)
            {
              command = null;
              return null;
            }

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

            return null;
        }

        protected override bool TryAttack(
            Enemy enemy,
            Character player,
            out ICommand? command)
        {
            command = null;

            if (enemy.Weapon is Dagger dagger &&
                enemy.GetDistanceTo(player) <= dagger.MaxRange &&
                enemy.attackTick <= 0)
            {
                enemy.attackTick = 5;

                Log.Debug("{enemy} attacks {player} with {weapon}",
                    enemy, player, dagger);

                command = new UseWeaponCommand(enemy.Identity);
                return true;
            }

            return false;
        }
    }

    internal sealed class DeepSplitStrategy : EnemyStrategy
    {
        protected override ICommand? TrySpecialAction(Enemy enemy, Character player)
        {

            if (enemy.Dead)
            {
              command = null;
              return null;
             };

            Character? nearestPlayer = enemy.GetClosestOpponent();
            // Log.Debug("Nearest player for {zombie} is {nearestPlayer}", enemy, nearestPlayer);
            if (nearestPlayer is null)
            {
              command = null;
              return null;
            }

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

            return null;
        }

        protected override bool TryAttack(
            Enemy enemy,
            Character player,
            out ICommand? command)
        {
            command = null;

            if (enemy.Weapon is Dagger dagger &&
                enemy.GetDistanceTo(player) <= dagger.MaxRange &&
                enemy.attackTick <= 0)
            {
                enemy.attackTick = 5;
                command = new UseWeaponCommand(enemy.Identity);
                return true;
            }

            return false;
        }
    }
}


