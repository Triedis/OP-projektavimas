using System.Diagnostics;
using Serilog;

class TerminalRenderer
{
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

        Player? player = state.players.FirstOrDefault(p => p.Username == state.Identity?.Username);
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
        foreach (Skeleton skeleton in state.skeletons.Where(s => s.Room.Equals(currentRoom)))
        {
            ConsoleColor color;
            if (!skeleton.Dead) {
                color = ConsoleColor.Red;
            } else {
                color = ConsoleColor.Gray;
            }
            Console.ForegroundColor = color;
            Console.SetCursorPosition(skeleton.PositionInRoom.X, skeleton.PositionInRoom.Y);
            Console.Write('S');
        }

        foreach (var p in state.players.Where(p => p.Room.Equals(state.Identity?.Room)))
        {
            Console.ForegroundColor = (ConsoleColor)p.Color;
            Console.SetCursorPosition(p.PositionInRoom.X, p.PositionInRoom.Y);
            Console.Write('@');
        }

        Console.SetCursorPosition(0, currentRoom.Shape.Y + 1);
        Console.ResetColor();

        // Unimportant debug logging.
        // foreach (var p in state.players)
        // {
        //     Console.WriteLine($"plr:{p.Identity}");
        //     Console.WriteLine($"-sameRoom:{p.Room.Equals(state.Identity?.Room)}");
        // }

        // foreach (var p in state.skeletons)
        // {
        //     Console.WriteLine($"skl:{p.Identity}");
        //     Console.WriteLine($"-sameRoom:{p.Room.Equals(state.Identity?.Room)}");
        // }


        // Console.WriteLine($"room:{currentRoom.WorldGridPosition}");
        // Console.WriteLine($"-numBoundaryPoints:{currentRoom.BoundaryPoints.Count}");

        foreach (string message in state.MessagesToDisplay) {
            Console.WriteLine(message);
        }
    }
}