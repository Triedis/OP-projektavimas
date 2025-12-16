


using Serilog;

class DeadState : IClientState
{
    public void Enter(ClientStateController client)
    {
        Log.Warning("PLAYER STATE: dead");
        Console.WriteLine($"You are dead");
    }

    public Task HandleInput(ClientStateController clinet, ConsoleKey key)
    {
        if (key == ConsoleKey.Q)
        {
            Environment.Exit(0);
        }
        return Task.CompletedTask;
    }

    public Task Update(ClientStateController client)
    {
        return Task.CompletedTask;
    }
    public void Exit(ClientStateController client)
    {
        
    }
}