public interface IClientState
{
    void Enter(ClientStateController clinet);
    Task Update(ClientStateController client);
    Task HandleInput(ClientStateController clinet, ConsoleKey key);
    void Exit(ClientStateController client);
}