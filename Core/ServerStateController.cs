using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;

class ServerStateController : IStateController
{
    private int _port;
    private readonly Queue<ICommand> _receivedCommands = new();
    private readonly Dictionary<string, NetworkStream> _clients = new();

    public ServerStateController(int port)
    {
        this._port = port;
    }

    public override async Task Run()
    {
        var clientTask = ListenForClients();
        var serverTask = GameLoop();

        await Task.WhenAll(clientTask, serverTask);
    }

    private async Task GameLoop()
    {

        while (true)
        {
            Console.WriteLine("Running tick");
            while (_receivedCommands.Count > 0)
            {
                ICommand command = _receivedCommands.Dequeue();
                command.ExecuteOnServer(this);
            }
            await Task.Delay(1000);
        }
    }

    public void SendCommand(ICommand command, NetworkStream clientStream)
    {
        Console.WriteLine(command);
        Type commandType = command.GetType();
        var wrapper = new PacketWrapper
        {
            TypeName = commandType.AssemblyQualifiedName!,
            JsonPayload = JsonSerializer.Serialize(command, commandType, NetworkSerializer.Options)
        };

        string jsonString = JsonSerializer.Serialize(wrapper, NetworkSerializer.Options);
        // Console.WriteLine(jsonString);
        byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);

        try
        {
            clientStream.Write(jsonBytes, 0, jsonBytes.Length);
            clientStream.Flush();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to repliate to client. Is it still alive?\n{e}");
        }
    }

    private async Task ListenForClients()
    {

        TcpListener listener;
        try
        {
            listener = new TcpListener(IPAddress.Any, _port);
            listener.Start();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return;
        }

        Console.WriteLine($"Running TCP server on {_port}");
        while (true)
        {
            TcpClient client = await listener.AcceptTcpClientAsync();
            string clientAddress = client.Client.RemoteEndPoint!.ToString()!;
            Console.WriteLine($"Received client connection ... {clientAddress}");

            _clients.Add(clientAddress, client.GetStream());
            Console.WriteLine("Replicating current state ...");
            SendCommand(GetSnapshotCommand(), client.GetStream());
            await ListenForClient(client);
        }
    }

    private ICommand GetSnapshotCommand()
    {
        GameStateSnapshot snapshot = new GameStateSnapshot(players, skeletons, worldGrid, console);
        return new SyncCommand(snapshot);
    }

    private async Task ListenForClient(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];
        int bytesRead;

        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
        {
            string jsonString = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            ICommand? command = JsonSerializer.Deserialize<ICommand>(jsonString);
            if (command is null)
            {
                Console.WriteLine("Malformed message from client?");
                continue;
            }

            EnqueueCommand(command);
        }
    }

    private void EnqueueCommand(ICommand command)
    {
        _receivedCommands.Append(command);
    }
}
