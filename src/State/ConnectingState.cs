

using Serilog;

class ConnectingState : IClientState
{
    public void Enter(ClientStateController clinet)
    {
        Log.Information("PLAYER STATE: connecting");
        Console.WriteLine("Connecting . . .");
    }

    public Task HandleInput(ClientStateController clinet, ConsoleKey key)
    {
        return Task.CompletedTask;
    }

    public async Task Update(ClientStateController clinet)
    {
        if(clinet.Identity != null)
        {
            clinet.Mediator.ChangeState(new SyncingState());
        }
        await Task.CompletedTask;
    }
    public void Exit(ClientStateController clinet)
    {
        Console.WriteLine("Connected . . .");
    }
}