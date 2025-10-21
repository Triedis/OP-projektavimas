using System.Text.Json.Serialization;
using Serilog;

class MoveCommand : ICommand
{
    public Vector2 Position { get; set; }
    public Guid ActorIdentity { get; set; }

    [JsonConstructor]
    public MoveCommand(Vector2 Position, Guid ActorIdentity) {
        this.Position = Position;
        this.ActorIdentity = ActorIdentity;
    }

    public async Task ExecuteOnClient(ClientStateController gameState)
    {
        await gameState.SendCommand(this);
    }

    public async Task ExecuteOnServer(ServerStateController gameState)
    {
        await Task.Run(() =>
        {
            Character? target = gameState.players.Cast<Character>().Concat(gameState.enemies).FirstOrDefault((character) => character.Identity.Equals(ActorIdentity));
            if (target is null)
            {
                Console.WriteLine("Failed to replicate movement on server. Nil.");
                return;
            }

            var pos = Position;
            var room = target.Room;
            var roomx = room.Shape.X;
            var roomy = room.Shape.Y;

            Console.WriteLine($"MoveCommand exec: pos={pos},char={ActorIdentity},rs={room.Shape}");

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
                    if (occupant.Identity.Equals(ActorIdentity))
                    {
                        continue;
                    }

                    if (occupant.PositionInRoom.Equals(pos) && !occupant.Dead)
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
