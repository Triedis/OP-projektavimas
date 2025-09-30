
class SyncCommand : ICommand
{
    public GameStateSnapshot Snapshot;

    public SyncCommand(GameStateSnapshot snapshot)
    {
        Snapshot = snapshot;
    }

    public void ExecuteOnClient(ClientStateController gameState)
    {
        gameState.ApplySnapshot(Snapshot);
    }

    public void ExecuteOnServer(ServerStateController gameState)
    {
        throw new NotImplementedException();
    }
}
