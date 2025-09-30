using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

class ClientStateController : IStateController
{
    private TcpClient _client;
    private NetworkStream _stream;

    public ClientStateController(string ipAddress, int port)
    {
        _client = new TcpClient();
        _client.Connect(ipAddress, port);
        _stream = _client.GetStream();
    }

    public override async Task Run()
    {
        await ListenForServer();
    }

    private async Task ListenForServer()
    {
        byte[] buffer = new byte[4096];
        int bytesRead;

        Console.WriteLine("Listening for server ...");
        while ((bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
        {
            string jsonWrapperString = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Console.WriteLine(jsonWrapperString);
            PacketWrapper? wrapper = JsonSerializer.Deserialize<PacketWrapper>(jsonWrapperString);
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

            ICommand? command = JsonSerializer.Deserialize(wrapper.JsonPayload, commandType, NetworkSerializer.Options) as ICommand;
            if (command is null)
            {
                Console.WriteLine("Failed to deserialize command payload.");
                continue;
            }

            Console.WriteLine($"Executing command from server of type {command.GetType()}");
            command.ExecuteOnClient(this);
        }
    }

    public void ApplySnapshot(GameStateSnapshot snapshot)
    {
        players = snapshot.Players;
        skeletons = snapshot.Skeletons;
        worldGrid = snapshot.WorldGrid;
        console = snapshot.GameConsole;
    }

    public void SendCommand(ICommand command)
    {
        string jsonString = JsonSerializer.Serialize(command, command.GetType());
        byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);

        try
        {
            _stream.Write(jsonBytes, 0, jsonBytes.Length);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to repliate to server. Is it still running?\n{e}");
            Console.WriteLine($"This might cause serious desync.");
        }
    }
}
