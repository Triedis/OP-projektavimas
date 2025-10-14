using Serilog;

class MoveCommand(Vector2 position, Character character) : ICommand
{
    public Vector2 Position = position;
    public Character Character = character;

    public async Task ExecuteOnClient(ClientStateController gameState)
    {
        await gameState.SendCommand(this);
    }

    public async Task ExecuteOnServer(ServerStateController gameState)
    {
        await Task.Run(() =>
        {
            Character? target = gameState.players.Cast<Character>().Concat(gameState.skeletons).FirstOrDefault((character) => character.Equals(Character));
            if (target is null)
            {
                Console.WriteLine("Failed to replicate movement on server. Nil.");
                return;
            }

            var pos = Position;
            var room = target.Room;
            var roomx = room.Shape.X;
            var roomy = room.Shape.Y;

            Console.WriteLine($"MoveCommand exec: pos={pos},char={Character.Identity},rs={room.Shape}");

            bool inBoundsX = pos.X >= 1 && pos.X <= roomx - 2;
            bool inBoundsY = pos.Y >= 1 && pos.Y <= roomy - 2;
            bool inBounds = inBoundsX && inBoundsY;

            KeyValuePair<Direction, RoomBoundary> exitDirValuePair = room.BoundaryPoints.Where(pair => pair.Value.PositionInRoom.Equals(pos)).FirstOrDefault();

            Log.Debug("Movement in bounds? {inBounds}", inBounds);
            Log.Debug("Moving to exit? {exitDirValuePair}", exitDirValuePair.Value is not null);
            if (inBounds)
            {
                IEnumerable<Character> occupants = room.Occupants;
                bool clear = true;
                foreach (Character occupant in occupants)
                {
                    if (occupant.Equals(Character))
                    {
                        continue;
                    }

                    if (occupant.PositionInRoom.Equals(pos))
                    {
                        clear = false;
                    }
                }

                if (clear) {
                    Log.Debug("Moving in room to {pos}", pos);
                    target.SetPositionInRoom(pos);
                    Console.ForegroundColor = ConsoleColor.White;

                }

            }
            else if (exitDirValuePair.Value is not null)
            {
                Vector2 offsetPosition = room.WorldGridPosition + DirectionUtils.GetVectorDirection(exitDirValuePair.Key);
                Console.WriteLine($"Trying to enter room at {offsetPosition}");
                Room? newRoom = gameState.worldGrid.GetRoom(offsetPosition);
                if (newRoom is null)
                {

                    Console.WriteLine("Room is null ... Creating.");
                    newRoom = gameState.worldGrid.GenRoom(offsetPosition);
                }
                Direction enteringFrom = DirectionUtils.GetOpposite(exitDirValuePair.Key);
                Console.WriteLine($"Entering room from {enteringFrom}");
                newRoom.Enter(target, enteringFrom);

                return;
            }
        });
    }
}
