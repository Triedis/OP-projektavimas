using Serilog;
using System.Linq;

class UseWeaponCommand(Guid actorIdentity) : ICommand
{
    public Guid ActorIdentity { get; } = actorIdentity;

    public async Task ExecuteOnClient(ClientStateController gameState)
    {
        await gameState.SendCommand(this);
    }

    public Task ExecuteOnServer(ServerStateController gameState)
    {
        Log.Information("UseWeaponCommand::ExecuteOnServer from {actorIdentity}", ActorIdentity);

        if (gameState.Game is null)
        {
            Log.Error("GameFacade is not initialized in ServerStateController");
            return Task.CompletedTask;
        }

        _ = gameState.Game.UseWeapon(ActorIdentity);

        return Task.CompletedTask;
    }
}