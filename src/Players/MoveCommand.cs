using System.Threading.Tasks;

class MoveCommand(Vector2 position, Character character) : ICommand
{
    public Vector2 Position = position;
    public Character Character = character;

    public async Task ExecuteOnClient(ClientStateController gameState)
    {
        await gameState.SendCommand(this);
    }

    public async Task ExecuteOnServer(ServerStateController gameState)
    {
        await Task.Run(() =>
        {
            Console.WriteLine($"Mock server execution of MoveCommand: pos={Position},char={Character.identity}");
            Character? target = gameState.players.FirstOrDefault((player) => player.Equals(Character));
            if (target is null) {
                Console.WriteLine("Failed to replicate movement on server. Nil.");
            } else {
                target.SetPositionInRoom(Position);

            }
        });
    }
}
