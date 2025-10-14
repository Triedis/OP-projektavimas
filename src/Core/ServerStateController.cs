using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Xml.Schema;
using Serilog;

class ServerStateController(int port) : IStateController
{
    private readonly Vector2 _initialRoomPosition = new(0, 0);
    private readonly int _port = port;
    private readonly Queue<ICommand> _receivedCommands = [];
    private readonly Dictionary<Guid, NetworkStream> _clients = [];
    private readonly Random rng = new(); // should be a singleton for consistency.

    public override async Task Run()
    {
        try
        {
            Room _ = worldGrid.GenRoom(_initialRoomPosition);
            Skeleton testSkeleton = new(Guid.NewGuid(), _, new(2, 2), new(1, 1));
            skeletons.Add(testSkeleton);
            _.Enter(testSkeleton);

            var clientTask = ListenForClients();
            var serverTask = GameLoop();

            await Task.WhenAll(clientTask, serverTask);
        }
        catch (Exception e)
        {
            Console.WriteLine($"?: {e}");
        }
    }

    private async Task GameLoop()
    {

        while (true)
        {
            RunAI();
            await ExecuteClientCommands();

            await SyncAll(); // ideally should be a delta update ...
            await Task.Delay(500);
        }
    }

    private async Task ExecuteClientCommands()
    {
        if (_receivedCommands.Count > 0)
        {
            while (_receivedCommands.Count > 0)
            {
                try
                {
                    ICommand command = _receivedCommands.Dequeue();
                    Log.Information($"Executing {command.GetType()}");

                    await command.ExecuteOnServer(this);

                }
                catch (Exception e)
                {
                    Log.Error($"Failed to execute command from queue: {e}");
                }
            }


        }

    }

    private void RunAI()
    {
        Log.Debug("Ticking AI with {count} entities", skeletons.Count);
        foreach (Skeleton skeleton in skeletons)
        {
            ICommand? command = skeleton.TickAI();

            if (command is not null) {
                Log.Debug("Entity {skeleton} decided to {command}", skeleton, command.GetType());
            }
            command?.ExecuteOnServer(this);
        }
    }

    public async Task SendCommand(ICommand command, NetworkStream clientStream)
    {
        Log.Debug($"Sending of type {command.GetType()}");
        var duplicateGroups = worldGrid.GetAllRooms()
            .GroupBy(room => room.WorldGridPosition)
            .Where(group => group.Count() > 1);

        if (duplicateGroups.Any())
        {
            foreach (var group in duplicateGroups)
            {
                Log.Warning($"Duplicate room found at position {group.Key}. Count: {group.Count()}");
            }
        }

        Type commandType = command.GetType();
        var wrapper = new PacketWrapper
        {
            TypeName = commandType.AssemblyQualifiedName!,
            JsonPayload = JsonSerializer.Serialize(command, commandType, NetworkSerializer.Options)
        };

        byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(wrapper, NetworkSerializer.Options);

        int messageLength = jsonBytes.Length; // required for length-prefixing. TCP can be annoying.
        byte[] lengthPrefix = BitConverter.GetBytes(messageLength);

        var b = Encoding.UTF8.GetString(jsonBytes, 0, messageLength);
        Log.Debug("sending {b}", b);

        try
        {
            await clientStream.WriteAsync(lengthPrefix);
            await clientStream.WriteAsync(jsonBytes);
            await clientStream.FlushAsync();
        }
        catch
        {
            Log.Debug($"Failed to repliate to client. Is it still alive?");
            throw;
        }

        Log.Debug($"Sent of type {command.GetType()}");
    }

    private async Task ListenForClients()
    {

        TcpListener listener = new(IPAddress.Any, _port);
        listener.Start();

        List<Task> clientTasks = [];

        Log.Information($"Running TCP server on {_port}");
        while (true)
        {
            TcpClient client = await listener.AcceptTcpClientAsync();
            clientTasks.Add(HandleClient(client));
        }
    }

    public async Task HandleClient(TcpClient client)
    {
        string? username = client.Client.RemoteEndPoint?.ToString();
        if (username is null) {
            return;
        }
        Guid clientIdentity = Guid.NewGuid();

        // Register for replication
        _clients.Add(clientIdentity, client.GetStream());
        Log.Information($"Received client connection ... {clientIdentity}");

        Player _ = AddPlayer(clientIdentity, username);
        try {
            await SendCommand(GetSnapshotCommand(clientIdentity), client.GetStream());
            await ListenForClient(client);
        } catch (Exception e) {
            Log.Warning("Looks like {e} really would just brick the server ...", e);
        }
    }

    private async Task SyncAll()
    {
        foreach (KeyValuePair<Guid, NetworkStream> entry in _clients)
        {
            Guid clientIdentity = entry.Key;
            NetworkStream clientStream = entry.Value;

            SyncCommand cmd = GetSnapshotCommand(clientIdentity);
            try
            {
                await SendCommand(cmd, clientStream);
            }
            catch
            {
                Log.Warning("Failed to replicate to client {client}. Disconnecting.", clientIdentity);
                Disconnect(clientIdentity);
            }
        }
    }

    private void Disconnect(Guid identity)
    {
        Player? player = players.Where(player => player.Identity == identity).FirstOrDefault();
        
        if (player is null) {
            Log.Warning("Attempted to disconnect client {identity} which is not bound to any player object");
            return;
        }

        LogEntry playerLeaveEntry = LogEntry.ForGlobal($"Player {player.Username} has departed.");
        MessageLog.Instance.Add(playerLeaveEntry);

        player.Destroy(); // destroy is called to remove the character from any rooms
        players.Remove(player);
        _clients.Remove(identity);
    }

    private Player AddPlayer(Guid identity, string username)
    {
        Room? initialRoom = worldGrid.GetRoom(_initialRoomPosition);
        if (initialRoom is null)
        {
            Log.Error("Initial room missing?");
            throw new InvalidOperationException();
        }

        Vector2 initialRoomShape = initialRoom.Shape;
        Vector2 middlePosition = new(initialRoomShape.X / 2, initialRoomShape.Y / 2);
        Sword weapon = new(1, 25);
        Array colorValues = typeof(Color).GetEnumValues();
        Color randomColor = (Color?)colorValues.GetValue(rng.Next(colorValues.Length)) ?? throw new InvalidOperationException();
        Player player = new(username, identity, randomColor, initialRoom, middlePosition, weapon);
        initialRoom.Enter(player);

        players.Add(player);

        LogEntry playerJoinEntry = LogEntry.ForGlobal($"Player {username} has appeared.");
        MessageLog.Instance.Add(playerJoinEntry);

        return player;
    }

    private SyncCommand GetSnapshotCommand(Guid clientIdentity)
    {
        GameStateSnapshot snapshot = new(players, skeletons, worldGrid);
        return new SyncCommand(snapshot, clientIdentity);
    }

    private async Task ListenForClient(TcpClient client)
    {
        var _log = Log.ForContext("client", client.Client.RemoteEndPoint);
        _log.Debug("Listening ...");

        NetworkStream stream = client.GetStream();

        byte[] lengthBuffer = new byte[4];

        while (true) {
            await stream.ReadExactlyAsync(lengthBuffer, 0, 4);
            int messageLength = BitConverter.ToInt32(lengthBuffer, 0);

            byte[] messageBuffer = new byte[messageLength];
            await stream.ReadExactlyAsync(messageBuffer, 0, messageLength);

            string jsonWrapperString = Encoding.UTF8.GetString(messageBuffer);
            try
            {
                PacketWrapper? wrapper = JsonSerializer.Deserialize<PacketWrapper>(jsonWrapperString, NetworkSerializer.Options);
                if (wrapper is null || wrapper.TypeName is null || wrapper.JsonPayload is null)
                {
                    _log.Warning("Malformed wrapper from client?");
                    continue;
                }

                Type? commandType = Type.GetType(wrapper.TypeName);
                if (commandType is null)
                {
                    _log.Warning($"Unknown command type from client: {wrapper.TypeName}");
                    continue;
                }

                if (JsonSerializer.Deserialize(wrapper.JsonPayload, commandType, NetworkSerializer.Options) is not ICommand command)
                {
                    _log.Warning("Failed to deserialize command payload.");
                    continue;
                }

                _log.Debug($"Executing command from client of type {command.GetType()}");
                EnqueueCommand(command);
            }
            catch (JsonException e)
            {
                _log.Warning(e, "Failed to deserialize client packet.");
            }
        }
    }


    private void EnqueueCommand(ICommand command)
    {
        _receivedCommands.Enqueue(command);
    }
}
