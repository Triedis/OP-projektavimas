
class SyncCommand(GameStateSnapshot snapshot, string identity) : ICommand
{
    public GameStateSnapshot Snapshot = snapshot;
    public string Identity = identity;

    public async Task ExecuteOnClient(ClientStateController gameState)
    {
        Console.WriteLine($"Received new identity {Identity}");
        gameState.ApplySnapshot(Snapshot);
        gameState.SetIdentity(Identity); // Must happen after the snapshot brings in all players
    }

    public async Task ExecuteOnServer(ServerStateController gameState)
    {
        Console.WriteLine("you've hit a terrible stub");
        throw new NotImplementedException();
    }
}
