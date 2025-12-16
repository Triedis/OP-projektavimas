using System.ComponentModel.DataAnnotations;

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

    /// <summary>
    /// Performs a piecewise multiplication of two vectors.
    /// </summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>A new vector with components that are the product of the input vectors' components.</returns>
    public static Vector2 MulPiecewise(Vector2 left, Vector2 right)
    {
        return new Vector2(left.X * right.X, left.Y * right.Y);
    }

    public override bool Equals(object? obj)
    {
        return obj is Vector2 other && X == other.X && Y == other.Y;
    }

    public override int GetHashCode()
    {
        int hash = 17;
        hash = hash * 23 + X.GetHashCode();
        hash = hash * 23 + Y.GetHashCode();
        return hash;
    }

    public override string? ToString()
    {
        return $"x={X};y={Y}";
    }

    /// <summary>
    /// Creates a Vector2 from a string formatted as "x,y".
    /// </summary>
    /// <param name="key">The string to parse.</param>
    /// <returns>A new Vector2 parsed from the string.</returns>
    public static Vector2 FromKeyString(string key)
    {
        var parts = key.Split(',');
        return new Vector2(int.Parse(parts[0]), int.Parse(parts[1]));
    }

    /// <summary>
    /// Swaps the X and Y components of a vector.
    /// </summary>
    /// <param name="vec">The vector to reverse.</param>
    /// <returns>A new vector with the X and Y components swapped.</returns>
    public static Vector2 Reverse(Vector2 vec)
    {
        return new(vec.Y, vec.X);
    }

    /// <summary>
    /// Negates the X component to convert to a console screen representation from a Cartesian system.
    /// </summary>
    /// <returns>A new vector with the X component negated.</returns>
    public Vector2 ToScreenSpace() {
        return new(-X, Y);
    }

    /// <summary>
    /// Calculates the distance between two vectors.
    /// </summary>
    /// <returns>The Euclidean distance between the two vectors.</returns>
    public static double Distance(Vector2 a, Vector2 b)
    {
        double dx = a.X - b.X;
        double dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
