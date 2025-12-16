public interface IClientMediator
{
    Task NotifyInput(ConsoleKey key);
    Task NotifyServerCommand(ICommand command);
    void ChangeState(IClientState newState);
    void RequestRender();
}