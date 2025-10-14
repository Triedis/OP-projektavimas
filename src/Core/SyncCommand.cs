
using Serilog;

class SyncCommand(GameStateSnapshot snapshot, Guid identity) : ICommand
{
    public GameStateSnapshot Snapshot = snapshot;
    public Guid Identity = identity;

    public Task ExecuteOnClient(ClientStateController gameState)
    {
        Log.Debug("Received new identity {Identity}", Identity);
        gameState.ApplySnapshot(Snapshot);
        gameState.SetIdentity(Identity); // Must happen after the snapshot brings in all players
        return Task.CompletedTask;
    }

    public Task ExecuteOnServer(ServerStateController gameState)
    {
        Console.WriteLine("SyncCommand::ExecuteOnServer should not be called on the server.");
        throw new InvalidOperationException("SyncCommand::ExecuteOnServer should not be called on the server.");
    }
}
