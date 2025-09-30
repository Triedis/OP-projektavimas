using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;
using System.ComponentModel;


class ServerStateController : IStateController
{
    private Vector2 _initialRoomPosition = new(0, 0);
    private int _port;
    private Queue<ICommand> _receivedCommands = [];
    private Dictionary<string, NetworkStream> _clients = [];

    public ServerStateController(int port)
    {
        this._port = port;
    }

    public override async Task Run()
    {
        Room _ = worldGrid.GenRoom(_initialRoomPosition);


        var clientTask = ListenForClients();
        var serverTask = GameLoop();

        await Task.WhenAll(clientTask, serverTask);
    }

    private async Task GameLoop()
    {

        while (true)
        {
            if (_receivedCommands.Count > 0)
            {
                while (_receivedCommands.Count > 0)
                {
                    try
                    {
                        ICommand command = _receivedCommands.Dequeue();
                        Console.WriteLine($"Executing {command.GetType()}");

                        await command.ExecuteOnServer(this);

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Failed to execute command from queue: {e}");
                    }
                }

                await SyncAll(); // horrible

            }

            await Task.Delay(1000);
        }
    }

    public static async Task SendCommand(ICommand command, NetworkStream clientStream)
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
            // Console.WriteLine(jsonString);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);

            try
            {
                await clientStream.WriteAsync(jsonBytes);
                await clientStream.FlushAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to repliate to client. Is it still alive?\n{e}");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"fuck... {e}");
        }


        Console.WriteLine($"Sent of type {command.GetType()}");
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

            // Address:port will be the universal identifier
            string clientIdentity = client.Client.RemoteEndPoint!.ToString()!;

            // Register for replication
            _clients.Add(clientIdentity, client.GetStream());
            Console.WriteLine($"Received client connection ... {clientIdentity}");

            Console.WriteLine("Replicating current state ...");
            Player _ = AddPlayer(clientIdentity);
            await SendCommand(GetSnapshotCommand(clientIdentity), client.GetStream());
            await ListenForClient(client);
        }
    }

    private async Task SyncAll()
    {
        foreach (KeyValuePair<string, NetworkStream> entry in _clients)
        {
            SyncCommand cmd = GetSnapshotCommand(entry.Key);
            await SendCommand(cmd, entry.Value);
        }
    }

    private Player AddPlayer(string clientIdentity)
    {
        Room initialRoom = worldGrid.GetRoom(_initialRoomPosition);
        Sword weapon = new(1, 25);
        Player player = new(clientIdentity, Color.Blue, initialRoom, _initialRoomPosition, weapon);
        initialRoom.Enter(player);

        players.Add(player);

        return player;
    }

    private SyncCommand GetSnapshotCommand(string clientIdentity)
    {
        GameStateSnapshot snapshot = new GameStateSnapshot(players, skeletons, worldGrid, console);
        return new SyncCommand(snapshot, clientIdentity);
    }

    private async Task ListenForClient(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[4096];
        int bytesRead;

        Console.WriteLine("Listening for client ...");
        while ((bytesRead = await stream.ReadAsync(buffer)) != 0)
        {
            string jsonWrapperString = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            // Console.WriteLine($"shell: {jsonWrapperString}");
            try
            {
                PacketWrapper? wrapper = JsonSerializer.Deserialize<PacketWrapper>(jsonWrapperString, NetworkSerializer.Options);
                if (wrapper is null || wrapper.TypeName is null || wrapper.JsonPayload is null)
                {
                    Console.WriteLine("Malformed wrapper from client?");
                    continue;
                }

                Type? commandType = Type.GetType(wrapper.TypeName);
                if (commandType is null)
                {
                    Console.WriteLine($"Unknown command type from client: {wrapper.TypeName}");
                    continue;
                }

                if (JsonSerializer.Deserialize(wrapper.JsonPayload, commandType, NetworkSerializer.Options) is not ICommand command)
                {
                    Console.WriteLine("Failed to deserialize command payload.");
                    continue;
                }

                Console.WriteLine($"Executing command from client of type {command.GetType()}");

                EnqueueCommand(command);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to deserialize client packet ... {e}");
            }
        }
    }


    private void EnqueueCommand(ICommand command)
    {
        _receivedCommands.Enqueue(command);
    }
}
