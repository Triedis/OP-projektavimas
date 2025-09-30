public class Vector2(int x, int y)
{
    public int X { get; set; } = x;
    public int Y { get; set; } = y;

    public static Vector2 operator -(Vector2 left, Vector2 right) {
        return new Vector2(left.X - right.X, left.Y - right.Y);
    }

    public static Vector2 operator +(Vector2 left, Vector2 right) {
        return new Vector2(left.X + right.X, left.Y + right.Y);
    }

    public override bool Equals(object? obj)
    {
        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public override string? ToString()
    {
        return $"x={X};y={Y}";
    }
}
