
// The Concrete Visitor now generates a detailed 5x5 color-coded tile for each room.
internal class MinimapIconRenderVisitor : IRoomVisitor
{
    public MinimapTile[,] Tile { get; private set; } = new MinimapTile[5, 5];
    private bool _isCurrentRoom;

    public void SetContext(bool isCurrentRoom)
    {
        _isCurrentRoom = isCurrentRoom;
    }

    private void GenerateBaseTile(Room room, char symbol, ConsoleColor symbolColor)
    {
        Tile = new MinimapTile[5, 5];
        for (int y = 0; y < 5; y++)
        {
            for (int x = 0; x < 5; x++)
            {
                if (y == 0 || y == 4 || x == 0 || x == 4)
                {
                    Tile[y, x] = new MinimapTile('#', ConsoleColor.DarkGray); // Wall
                }
                else
                {
                    Tile[y, x] = new MinimapTile(' ', ConsoleColor.Black); // Floor
                }
            }
        }

        // Place doors
        if (room.BoundaryPoints.ContainsKey(Direction.NORTH)) Tile[0, 2] = new MinimapTile('.', ConsoleColor.White);
        if (room.BoundaryPoints.ContainsKey(Direction.SOUTH)) Tile[4, 2] = new MinimapTile('.', ConsoleColor.White);
        if (room.BoundaryPoints.ContainsKey(Direction.WEST))  Tile[2, 0] = new MinimapTile('.', ConsoleColor.White);
        if (room.BoundaryPoints.ContainsKey(Direction.EAST))  Tile[2, 4] = new MinimapTile('.', ConsoleColor.White);

        // Place center symbol
        if (_isCurrentRoom)
        {
            Tile[2, 2] = new MinimapTile('@', ConsoleColor.Cyan);
        }
        else
        {
            Tile[2, 2] = new MinimapTile(symbol, symbolColor);
        }
    }

    public void Visit(StandardRoom room)
    {
        GenerateBaseTile(room, 'S', ConsoleColor.White);
    }

    public void Visit(TreasureRoom room)
    {
        GenerateBaseTile(room, 'T', ConsoleColor.Yellow);
    }

    public void Visit(BossRoom room)
    {
        GenerateBaseTile(room, 'B', ConsoleColor.Red);
    }
}
