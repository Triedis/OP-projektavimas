using System;

// A struct to hold the character and color for a single point on the minimap.
public struct MinimapTile
{
    public char Character { get; set; }
    public ConsoleColor Color { get; set; }

    public MinimapTile(char character, ConsoleColor color)
    {
        Character = character;
        Color = color;
    }
}
