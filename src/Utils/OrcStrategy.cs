//using OP_Projektavimas.Utils;
//using Serilog;

//class OrcStrategy : IStrategy
//{
//    private bool SeenPlayer = false;
//    private int locateDistance = 2;
//    public ICommand? TickAI(Enemy enemy)
//    {
//        if (enemy.Dead) {
//            return null;
//        }
//        Character? nearestPlayer = enemy.GetClosestOpponent();
//        Log.Debug("Nearest player for {orc} is {nearestPlayer}", this, nearestPlayer);
//        if (nearestPlayer is not null) {
//            int distance = enemy.GetDistanceTo(nearestPlayer);
//            if (distance <= locateDistance)
//            {
//                if (enemy.attackTick > 0) enemy.attackTick -= 1;

//                if (enemy.attackTick <= 0 && distance <= enemy.Weapon.MaxRange)
//                {
//                    enemy.attackTick = 5;
//                    return new UseWeaponCommand(enemy.Identity);
//                }
//            }

//            // Otherwise, move toward player
//            Vector2 direction = new(
//                nearestPlayer.PositionInRoom.X > enemy.PositionInRoom.X ? 1 :
//                nearestPlayer.PositionInRoom.X < enemy.PositionInRoom.X ? -1 : 0,
//                nearestPlayer.PositionInRoom.Y > enemy.PositionInRoom.Y ? 1 :
//                nearestPlayer.PositionInRoom.Y < enemy.PositionInRoom.Y ? -1 : 0
//            );

//            Vector2 newPosition = enemy.PositionInRoom + direction;
//            Log.Debug("{enemy} moves toward {player} to {newPos}", enemy, nearestPlayer, newPosition);

//            return new MoveCommand(newPosition, enemy.Identity);
//        }
//        return null;
//    }
//}