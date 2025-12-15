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
                        Character? target = gameState.players.Cast<Character>().Concat(gameState.enemies).FirstOrDefault(character => character.Identity.Equals(ActorIdentity));
                        if (target is null)
                        {
                            Log.Error("[MoveCommand] Failed to find actor with ID {ActorID}", ActorIdentity);
                            return;
                        }
                        if (target.Dead)
                        {
                            Log.Warning("[MoveCommand] Actor {ActorID} is dead and cannot move", ActorIdentity);
                            return;
                        }
        
                        var pos = Position;
                        var room = target.Room;
                        var roomx = room.Shape.X;
                        var roomy = room.Shape.Y;
        
                        Log.Information("[MoveCommand] Actor {Actor} attempting to move to {Position} in room {RoomID} ({RoomShape})",
                            target.Identity, pos, room.WorldGridPosition, room.Shape);
                    bool inBoundsX = pos.X >= 1 && pos.X <= roomx - 2;
                    bool inBoundsY = pos.Y >= 1 && pos.Y <= roomy - 2;
                    bool inBounds = inBoundsX && inBoundsY;

                    KeyValuePair<Direction, RoomBoundary> exitDirValuePair = room.BoundaryPoints.FirstOrDefault(pair => pair.Value.PositionInRoom.Equals(pos));

                    Log.Information("[MoveCommand] In-bounds check: {IsInBounds}. Exit check: {IsExit}", inBounds, exitDirValuePair.Value is not null);

                    if (inBounds)
                    {
                        // ... (rest of the in-room movement logic)
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

                        if (clear)
                        {
                            Log.Information("[MoveCommand] Path is clear. Moving actor {Actor} within room.", target.Identity);
                            target.SetPositionInRoom(pos);
                        }
                    }
                    else if (exitDirValuePair.Value is not null)
                    {
                        Direction exitDirection = exitDirValuePair.Key;
                        Vector2 offsetPosition = room.WorldGridPosition + DirectionUtils.GetVectorDirection(exitDirection);
                        Log.Information("[MoveCommand] Detected move to exit {ExitDirection}. New room should be at {NewRoomPos}", exitDirection, offsetPosition);

                        Room? newRoom = gameState.worldGrid.GetRoom(offsetPosition);
                        if (newRoom is null)
                        {
                            Log.Warning("[MoveCommand] Room at {NewRoomPos} does not exist. Creating it.", offsetPosition);
                            newRoom = gameState.CreateAndPopulateRoom(offsetPosition);
                            if (newRoom is null)
                            {
                                Log.Error("[MoveCommand] Failed to create new room at {NewRoomPos}. Aborting move.", offsetPosition);
                                MessageLog.Instance.Add(LogEntry.ForGlobal($"Failed to create new room at {offsetPosition}. Player movement halted."));
                                return; // Exit if room creation failed
                            }
                        }

                        Direction enteringFrom = DirectionUtils.GetOpposite(exitDirection);
                        Log.Information("[MoveCommand] Entering new room {NewRoomID} from direction {EnteringFrom}", newRoom.WorldGridPosition, enteringFrom);
                        newRoom.Enter(target, enteringFrom);
                    }
                });
    }
}
