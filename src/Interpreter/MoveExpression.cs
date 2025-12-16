namespace DungeonCrawler.src.Interpreter
{
    public class MoveExpression : Expression
    {
        private readonly Direction _direction;

        public MoveExpression(Direction direction)
        {
            _direction = direction;
        }

        public ICommand Interpret(GameContext context)
        {
            Vector2 newPosition =
                context.Player.PositionInRoom +
                DirectionUtils.GetVectorDirection(_direction);

            return new MoveCommand(
                newPosition,
                context.Player.Identity
            );
        }
    }
}
