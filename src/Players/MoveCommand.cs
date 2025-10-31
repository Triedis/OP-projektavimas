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
        if (gameState.Game is null)
        {
            Log.Error("GameFacade is not initialized in ServerStateController");
            return;
        }

        await gameState.Game.MovePlayer(ActorIdentity, Position);
    }
}
