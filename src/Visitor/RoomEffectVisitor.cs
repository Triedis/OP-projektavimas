using Serilog;

// Applies a real effect to occupants of a room by sending a command to the server.
internal class RoomEffectVisitor : IRoomVisitor
{
    private readonly ClientStateController _clientState;

    public RoomEffectVisitor(ClientStateController clientState)
    {
        _clientState = clientState;
    }

    public void Visit(StandardRoom room)
    {
        Log.Information($"Visiting StandardRoom at {room.WorldGridPosition}. No special effect applied.");
    }

    public void Visit(TreasureRoom room)
    {
        Log.Information($"Visiting TreasureRoom at {room.WorldGridPosition}. Activating 'Spike Trap'.");
        var command = new ApplyRoomEffectCommand(room.WorldGridPosition);
        command.ExecuteOnClient(_clientState); // This will send the command to the server
    }

    public void Visit(BossRoom room)
    {
        Log.Information($"Visiting BossRoom at {room.WorldGridPosition}. The boss's aura is menacing, but has no effect yet.");
    }
}
