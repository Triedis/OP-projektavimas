class SwingCommand : ICommand
{
    public Task ExecuteOnClient(ClientStateController gameState)
    {
        throw new NotImplementedException();
    }
    public Task ExecuteOnServer(ServerStateController gameState)
    {
        throw new NotImplementedException();
    }
}
