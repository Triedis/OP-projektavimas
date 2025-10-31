using System.Diagnostics;
using System.Dynamic;
using Serilog;

class TerminalRenderer
{
    private static readonly int RENDER_N_LAST_MESSAGES = 10;
    public static void Render(ClientStateController state)
    {
        Log.Debug("Hello, render");
        Console.Clear();
        List<string> log = [];

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
        if (currentRoom is null) {
            Log.Warning("No current room. Waiting for state update ...");
            return;
        }

        Console.ForegroundColor = ConsoleColor.Gray;
        for (int y = 0; y < currentRoom.Shape.Y; y++)
        {
            for (int x = 0; x < currentRoom.Shape.X; x++)
            {
                Vector2 position = new(x, y);
                if (y == 0 || y == currentRoom.Shape.Y - 1 || x == 0 || x == currentRoom.Shape.X - 1)
                {
                    log.Add($"calc room pixel {position}");
                    RoomBoundary? boundary = currentRoom.BoundaryPoints.Select(pair => pair.Value).Where(boundary => boundary.PositionInRoom.Equals(position)).FirstOrDefault();
                    Console.SetCursorPosition(x, y);
                    if (boundary is null) {
                        log.Add($"NO BOUND");
                        Console.Write('#');
                    } else {
                        log.Add($"BOUNDARY");
                        Console.Write('-');
                    }
                }
            }
        }

        Console.ForegroundColor = ConsoleColor.White;
        foreach (Enemy enemy in state.enemies.Where(s => s.Room.Equals(currentRoom)))
        {
            ConsoleColor color;
            if (!enemy.Dead)
            {
                color = ConsoleColor.Red;
            }
            else
            {
                color = ConsoleColor.Gray;
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

        }
        
        foreach(var l in currentRoom.LootDrops)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.SetCursorPosition(l.PositionInRoom.X, l.PositionInRoom.Y);

            char lootSymbol = 'L';
            if (l.Item is Sword)
            {
                lootSymbol = 'S';
            }
            else if (l.Item is Axe)
            {
                lootSymbol = 'A';
            }
            else if (l.Item is Dagger)
            {
                lootSymbol = 'D';
            }
            else if (l.Item is Bow)
            {
                lootSymbol = 'B';
            }
            else
            {
                Log.Warning("Invalid loot type");
            }
            Console.Write(lootSymbol);
        }

        foreach (var p in state.players.Where(p => p.Room.Equals(state.Identity?.Room)))
        {
            Log.Information("{p} is being rendered at {@pos}", p, p.PositionInRoom);
            Console.ForegroundColor = (ConsoleColor)p.Color;
            Console.SetCursorPosition(p.PositionInRoom.X, p.PositionInRoom.Y);
            Console.Write('@');
        }

        Console.SetCursorPosition(0, currentRoom.Shape.Y + 1);
        Console.ResetColor();

        IReadOnlyList<string> MessagesToDisplay = [.. state.MessagesToDisplay.Reverse().Take(RENDER_N_LAST_MESSAGES)];
        int i = 0;
        foreach (string message in MessagesToDisplay) {
            if (i == RENDER_N_LAST_MESSAGES - 2) {
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            if (i == RENDER_N_LAST_MESSAGES - 1) {
                Console.ForegroundColor = ConsoleColor.DarkGray;
            }
            Console.WriteLine(message);
            i++;
        }
        Console.ForegroundColor = ConsoleColor.White;
    }
}