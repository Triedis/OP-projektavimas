using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

class ClientStateController : IStateController
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;

    public Player? Identity;

    public ClientStateController(string ipAddress, int port)
    {
        _client = new TcpClient();
        _client.Connect(ipAddress, port);
        _stream = _client.GetStream();
    }

    public override async Task Run()
    {
        var listenTask = ListenForServer();
        var gameLoopTask = GameLoop();
        var renderLoopTask = RenderLoop();

        await Task.WhenAll(listenTask, gameLoopTask, renderLoopTask);
    }

    public async Task RenderLoop() {
        TerminalRenderer renderer = new();

        while (true)
        {
            renderer.Render(this);
            await Task.Delay(30);
        }
    }

    private async Task GameLoop()
    {
        while (true)
        {
            if (Identity is null) {
                continue;
            }

            ConsoleKeyInfo key = Console.ReadKey(true);
            Vector2? moveDirection = null;
            switch (key.Key)
            {
                case ConsoleKey.W:
                    moveDirection = new Vector2(0, -1);
                    break;
                case ConsoleKey.S:
                    moveDirection = new Vector2(0, 1);
                    break;
                case ConsoleKey.A:
                    moveDirection = new Vector2(-1, 0);
                    break;
                case ConsoleKey.D:
                    moveDirection = new Vector2(1, 0);
                    break;
            }

            if (moveDirection is not null) {
                Vector2 movePosition = Identity.PositionInRoom + moveDirection!;
                Console.WriteLine($"moving to {movePosition}");
                MoveCommand command = new(movePosition, Identity);

                await command.ExecuteOnClient(this);
            }

            await Task.Delay(10);
        }
    }


    private async Task ListenForServer()
    {
        byte[] buffer = new byte[4096];
        int bytesRead;

        Console.WriteLine("Listening for server ...");
        while ((bytesRead = await _stream.ReadAsync(buffer)) != 0)
        {
            string jsonWrapperString = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            // Console.WriteLine($"shell: {jsonWrapperString}");
            PacketWrapper? wrapper = JsonSerializer.Deserialize<PacketWrapper>(jsonWrapperString, NetworkSerializer.Options);
            if (wrapper is null || wrapper.TypeName is null || wrapper.JsonPayload is null)
            {
                Console.WriteLine("Malformed wrapper from server?");
                continue;
            }

            Type? commandType = Type.GetType(wrapper.TypeName);
            if (commandType is null)
            {
                Console.WriteLine($"Unknown command type from server: {wrapper.TypeName}");
                continue;
            }

            if (JsonSerializer.Deserialize(wrapper.JsonPayload, commandType, NetworkSerializer.Options) is not ICommand command)
            {
                Console.WriteLine("Failed to deserialize command payload.");
                continue;
            }

            Console.WriteLine($"Executing command from server of type {command.GetType()}");
            await command.ExecuteOnClient(this);
        }
    }

    public void SetIdentity(string identity)
    {
        Identity = players.Find((player) => player.Username == identity);
        if (identity is null)
        {
            Console.WriteLine("bad server replication");
            Environment.Exit(1);
            return;
        }
        Console.WriteLine($"Identity updated to {identity}");
    }

    public void ApplySnapshot(GameStateSnapshot snapshot)
    {
        players = snapshot.Players;
        skeletons = snapshot.Skeletons;
        worldGrid = snapshot.WorldGrid;
        console = snapshot.GameConsole;

        foreach (var room in worldGrid.GetAllRooms())
        {
            room.Occupants.Clear();
        }

        var allCharacters = players.Cast<Character>().Concat(skeletons);
        foreach (var character in allCharacters)
        {
            character.Room?.Occupants.Add(character);
        }
    }

    public async Task SendCommand(ICommand command)
    {
        Console.WriteLine($"Sending of type {command.GetType()}");

        try
        {
            Type commandType = command.GetType();
            var wrapper = new PacketWrapper
            {
                TypeName = commandType.AssemblyQualifiedName!,
                JsonPayload = JsonSerializer.Serialize(command, commandType, NetworkSerializer.Options)
            };

            string jsonString = JsonSerializer.Serialize(wrapper, NetworkSerializer.Options);
            Console.WriteLine(jsonString);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);

            try
            {
                await _stream.WriteAsync(jsonBytes);
                await _stream.FlushAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to replicate to server. Is it still alive?\n{e}");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"fuck... {e}");
        }


        Console.WriteLine($"Sent of type {command.GetType()}");
    }

}
