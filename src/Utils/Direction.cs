
public static class DirectionUtils
{
    public static Direction GetOpposite(Direction direction) {
        Direction result = direction;
        switch (direction) {
            case Direction.NORTH: result = Direction.SOUTH; break;
            case Direction.EAST: result = Direction.WEST; break;
            case Direction.SOUTH: result = Direction.NORTH; break;
            case Direction.WEST: result = Direction.EAST; break;
        }
        return result;
    }
    public static Vector2 GetVectorDirection(Direction direction) {
        Vector2? result = null;
        switch (direction) {
            case Direction.NORTH: result = new Vector2(0, -1); break;
            case Direction.EAST: result = new Vector2(1, 0); break;
            case Direction.SOUTH: result = new Vector2(0, 1); break;
            case Direction.WEST: result = new Vector2(-1, 0); break;
        }
        return result!;
    }
}


public enum Direction
{
    NORTH,
    EAST,
    SOUTH,
    WEST,
}