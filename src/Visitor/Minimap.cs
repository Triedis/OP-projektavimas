using System.Text;

internal class Minimap
{
    private const int TileSize = 5;
    private const int ConnectorSize = 1;
    private const float RenderDistance = 2.5f; // Render rooms within this distance

    public MinimapTile[,] Render(WorldGrid worldGrid, Vector2 playerPosition)
    {
        var allRooms = worldGrid.GetAllRooms();
        var roomsInView = allRooms.Where(r => Vector2.Distance(r.WorldGridPosition, playerPosition) < RenderDistance).ToList();

        if (!roomsInView.Any())
        {
            return new MinimapTile[0, 0];
        }

        var renderer = new MinimapIconRenderVisitor();

        int minX = roomsInView.Min(r => r.WorldGridPosition.X);
        int maxX = roomsInView.Max(r => r.WorldGridPosition.X);
        int minY = roomsInView.Min(r => r.WorldGridPosition.Y);
        int maxY = roomsInView.Max(r => r.WorldGridPosition.Y);

        int gridWidth = (maxX - minX + 1) * (TileSize + ConnectorSize);
        int gridHeight = (maxY - minY + 1) * (TileSize + ConnectorSize);
        var grid = new MinimapTile[gridHeight, gridWidth];

        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                grid[y, x] = new MinimapTile(' ', ConsoleColor.Black);
            }
        }

        // Place each room's tile onto the grid
        foreach (var room in roomsInView)
        {
            renderer.SetContext(room.WorldGridPosition.Equals(playerPosition));
            ((IVisitableRoom)room).Accept(renderer);
            var tile = renderer.Tile;

            int gridX = (room.WorldGridPosition.X - minX) * (TileSize + ConnectorSize);
            int gridY = (room.WorldGridPosition.Y - minY) * (TileSize + ConnectorSize);

            for (int y = 0; y < TileSize; y++)
            {
                for (int x = 0; x < TileSize; x++)
                {
                    if (gridY + y < gridHeight && gridX + x < gridWidth)
                    {
                        grid[gridY + y, gridX + x] = tile[y, x];
                    }
                }
            }
        }

        // Draw connectors
        foreach (var room in roomsInView)
        {
            int baseX = (room.WorldGridPosition.X - minX) * (TileSize + ConnectorSize);
            int baseY = (room.WorldGridPosition.Y - minY) * (TileSize + ConnectorSize);

            // East connector
            if (room.BoundaryPoints.ContainsKey(Direction.EAST))
            {
                var neighborPos = room.WorldGridPosition + DirectionUtils.GetVectorDirection(Direction.EAST);
                if (allRooms.Any(r => r.WorldGridPosition.Equals(neighborPos)))
                {
                    if (baseX + TileSize < gridWidth) grid[baseY + 2, baseX + TileSize] = new MinimapTile('-', ConsoleColor.DarkGray);
                }
            }
            // South connector
            if (room.BoundaryPoints.ContainsKey(Direction.SOUTH))
            {
                var neighborPos = room.WorldGridPosition + DirectionUtils.GetVectorDirection(Direction.SOUTH);
                if (allRooms.Any(r => r.WorldGridPosition.Equals(neighborPos)))
                {
                    if (baseY + TileSize < gridHeight) grid[baseY + TileSize, baseX + 2] = new MinimapTile('|', ConsoleColor.DarkGray);
                }
            }
        }
        return grid;
    }
}

