using System.Diagnostics;
using Serilog;

class ClientMediator : IClientMediator
{
    private readonly ClientStateController _controller;

    public ClientMediator(ClientStateController controller)
    {
        _controller = controller;
    }

    public async Task NotifyInput(ConsoleKey key)
    {
        await _controller.HandleInput(key);
    }

    public async Task NotifyServerCommand(ICommand command)
    {
        if (command is SyncCommand sync)
        {
            Log.Debug($"sync state set {sync.GetType()}");
            _controller.currentSync = sync;
            ChangeState(new SyncingState());
            return;
        }
        await command.ExecuteOnClient(_controller);
    }

    public void ChangeState(IClientState newState)
    {
        Log.Debug("MEDIATOR STATE");
        _controller.SetState(newState);
    }

    public void RequestRender()
    {
        Log.Debug("MEDIATOR RENDER");
        TerminalRenderer.Render(_controller);
    }
}