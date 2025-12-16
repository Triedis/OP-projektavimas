using System.Diagnostics;
using System.Dynamic;
using Serilog;

class TerminalRenderer
{
    private static readonly int MAP_WIDTH = 50;
    private static readonly int MAP_HEIGHT = 20;
    private static readonly int LOG_HEIGHT = 15;
    private static readonly int RENDER_N_LAST_MESSAGES = LOG_HEIGHT;

    public static void Render(ClientStateController state)
    {
        Console.Clear();

        if (state.worldGrid == null)
        {
            Console.SetCursorPosition(0, 0);
            Log.Debug("Waiting for initial state ...");
            return;
        }

        Player? player = state.players.FirstOrDefault(p => p.Identity == state.Identity?.Identity);
        if (player == null || player.Room == null)
        {
            Log.Warning("Player not found in a room. Waiting for state update ...");
            return;
        }

        Room? currentRoom = player.Room;
        if (currentRoom is null)
        {
            Log.Warning("No current room. Waiting for state update ...");
            return;
        }

        RenderRoom(currentRoom);

        foreach (Enemy enemy in state.enemies.Where(s => s.Room.Equals(currentRoom)))
        {
            ConsoleColor color;
            if (!enemy.Dead)
            {
                color = ConsoleColor.Red;
            }
            else
            {
                color = ConsoleColor.DarkMagenta;
            }
            Console.ForegroundColor = color;
            Console.SetCursorPosition(enemy.PositionInRoom.X, enemy.PositionInRoom.Y);
            char enemySymbol = 'E';
            if (enemy is Skeleton)
            {
                enemySymbol = 'S';
            }
            else if (enemy is Zombie)
            {
                enemySymbol = 'Z';
            }
            else if (enemy is Orc)
            {
                enemySymbol = 'O';
            }
            else if (enemy is Slime)
            {
                enemySymbol = 'L';
            }
            else if (enemy is PlayerEnemyAdapter)
            {
                enemySymbol = 'A';
            }
            else
            {
                Log.Warning("Invalid enemy type");
            }
            Console.Write(enemySymbol);
            Console.ResetColor();
        }

        foreach (var l in currentRoom.LootDrops)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.SetCursorPosition(l.PositionInRoom.X, l.PositionInRoom.Y);

            char lootSymbol = 'L';
            if (l is WeaponLootDrop weaponLoot)
            {
                if (weaponLoot.Item is Sword)
                {
                    lootSymbol = 'S';
                }
                else if (weaponLoot.Item is Axe)
                {
                    lootSymbol = 'A';
                }
                else if (weaponLoot.Item is Dagger)
                {
                    lootSymbol = 'D';
                }
                else if (weaponLoot.Item is Bow)
                {
                    lootSymbol = 'B';
                }
                else
                {
                    Log.Warning("Invalid loot type");
                }
            }
            else if (l is StatLootDrop)
            {
                lootSymbol = '+';
            }
            Console.Write(lootSymbol);
            Console.ResetColor();
        }

        foreach (var p in state.players.Where(p => p.Room.Equals(state.Identity?.Room)))
        {
            bool isSelf = p.Equals(state.Identity);
            Console.ForegroundColor = (ConsoleColor)p.Color;
            Console.SetCursorPosition(p.PositionInRoom.X, p.PositionInRoom.Y);
            Console.Write('@');
            Console.ResetColor();
        }

        RenderMinimap(state);
        RenderLogMessages(state);
    }

    private static void RenderRoom(Room currentRoom)
    {
        Console.ForegroundColor = ConsoleColor.White;
        for (int y = 0; y < currentRoom.Shape.Y; y++)
        {
            for (int x = 0; x < currentRoom.Shape.X; x++)
            {
                Vector2 position = new(x, y);
                if (y == 0 || y == currentRoom.Shape.Y - 1 || x == 0 || x == currentRoom.Shape.X - 1)
                {
                    RoomBoundary? boundary = currentRoom.BoundaryPoints.Select(pair => pair.Value).Where(boundary => boundary.PositionInRoom.Equals(position)).FirstOrDefault();
                    Console.SetCursorPosition(x, y);
                    if (boundary is null)
                    {
                        Console.Write('#');
                    }
                    else
                    {
                        Console.Write('-');
                    }
                }
            }
        }
        Console.ResetColor();
    }

    private static void RenderMinimap(ClientStateController state)
    {
        var minimapGrid = state.MinimapDisplay;
        int gridHeight = minimapGrid.GetLength(0);
        int gridWidth = minimapGrid.GetLength(1);

        if (gridHeight > 0)
        {
            // Position the minimap at the top right
            int minimapStartX = Console.WindowWidth - gridWidth - 1;
            int minimapStartY = 0;

            for (int y = 0; y < gridHeight; y++)
            {
                Console.SetCursorPosition(minimapStartX, minimapStartY + y);
                for (int x = 0; x < gridWidth; x++)
                {
                    var tile = minimapGrid[y, x];
                    Console.ForegroundColor = tile.Color;
                    Console.Write(tile.Character);
                }
            }
        }
        Console.ResetColor();
    }

    private static void RenderLogMessages(ClientStateController state)
    {
        // Render Message Log below the main game view
        int logStartY = MAP_HEIGHT + 2; // Position below the main map
        Console.SetCursorPosition(0, logStartY);

        IReadOnlyList<string> messagesToDisplay = [.. state.MessagesToDisplay.Reverse().Take(RENDER_N_LAST_MESSAGES)];
        Console.ForegroundColor = ConsoleColor.Gray; // Distinct color for logs
        foreach (string message in messagesToDisplay)
        {
            Console.WriteLine(message);
        }
        Console.ResetColor();
    }
}
