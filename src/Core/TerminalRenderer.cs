class TerminalRenderer
{
    public void Render(ClientStateController state)
    {
        Console.Clear();

        if (state.worldGrid == null)
        {
            Console.SetCursorPosition(0, 0);
            Console.WriteLine("Waiting for initial state ...");
            return;
        }

        Player? player = state.players.FirstOrDefault(p => p.Username == state.Identity?.Username);
        if (player == null || player.Room == null)
        {
            Console.WriteLine("Player not found in a room. Waiting for state update ...");
            return;
        }

        Room currentRoom = player.Room;

        Console.ForegroundColor = ConsoleColor.Gray;
        for (int y = 0; y < currentRoom.Shape.Y; y++)
        {
            for (int x = 0; x < currentRoom.Shape.X; x++)
            {
                if (y == 0 || y == currentRoom.Shape.Y - 1 || x == 0 || x == currentRoom.Shape.X - 1)
                {
                    Console.SetCursorPosition(x, y);
                    Console.Write('#');
                }
            }
        }

        Console.ForegroundColor = ConsoleColor.White;
        foreach (var skeleton in state.skeletons.Where(s => s.Room.WorldGridPosition == currentRoom.WorldGridPosition))
        {
            Console.SetCursorPosition(skeleton.PositionInRoom.X, skeleton.PositionInRoom.Y);
            Console.Write('S');
        }

        foreach (var p in state.players.Where(p => p.Room.Equals(state.Identity?.Room)))
        {
            Console.ForegroundColor = (ConsoleColor)p.Color; // Simple cast for the prototype
            Console.SetCursorPosition(p.PositionInRoom.X, p.PositionInRoom.Y);
            Console.Write('@');
        }

        Console.SetCursorPosition(0, currentRoom.Shape.Y + 1);
        Console.ResetColor();

        foreach (var p in state.players)
        {
            Console.WriteLine($"plr:{p.identity}");
            Console.WriteLine($"-sameRoom:{p.Room.Equals(state.Identity?.Room)}");
        }
    }
}