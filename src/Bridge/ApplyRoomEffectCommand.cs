// Command to apply a status effect to all characters in a specific room.
// WIP/generally not needed.
class ApplyRoomEffectCommand : ICommand
{
    public Vector2 RoomPosition { get; }

    public ApplyRoomEffectCommand(Vector2 roomPosition)
    {
        RoomPosition = roomPosition;
    }

    public async Task ExecuteOnClient(ClientStateController client)
    {
        // This command is initiated by the client, so it only needs to send itself to the server.
        await client.SendCommand(this);
    }

    public async Task ExecuteOnServer(ServerStateController server)
    {
        Room? room = server.worldGrid.GetRoom(RoomPosition);
        if (room != null)
        {
            // Apply a bleeding effect to all characters in the room.
            foreach (var character in room.Occupants.ToList())
            {
                var bleedingStatus = new BleedingStatus(character, 5, 3); // 5 damage for 3 ticks
                server.RegisterOngoingEffect(bleedingStatus);
            }
        }
    }
}
