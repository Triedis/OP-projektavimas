class MoveCommand : ICommand
{
    private Vector2 _position;
    private Character _character;

    public MoveCommand(Vector2 position, Character character)
    {
        _position = position;
        _character = character;
    }

    public void ExecuteOnClient(ClientStateController gameState)
    {
        gameState.SendCommand(this);
    }

    public void ExecuteOnServer(ServerStateController gameState)
    {
        Console.WriteLine($"Mock server execution of MoveCommand: pos={_position},char={_character}");
    }
}
