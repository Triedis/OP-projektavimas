class ServerStateProxy : ServerStateController
{
    private ServerStateController? _realServer; // delayed creation
    private readonly HashSet<Guid> _authorizedClients = [];
	// ---------- DELAYED CREATION ----------
	private readonly int _port; // store port

	public ServerStateProxy(int port) : base(port)
	{
		_port = port; // store it for lazy creation
	}

	private ServerStateController RealServer
	{
		get
		{
			if (_realServer == null)
			{
				_realServer = new ServerStateController(_port); // pass port here
			}
			return _realServer;
		}
	}

	// ---------- SECURITY ----------
	public void AuthenticateClient(Guid clientId)
    {
        //Log.Information("Client {id} authenticated", clientId);
        _authorizedClients.Add(clientId);
    }

    // ---------- ADDED FUNCTIONALITY ----------
    public void EnqueueCommand(ICommand command, Guid clientId)
    {
        if (!_authorizedClients.Contains(clientId))
        {
            //Log.Warning("Unauthorized command attempt from {id}", clientId);
            return;
        }

        //Log.Debug("Proxy forwarding {cmd} from {id}", command.GetType(), clientId);

        // example validation hook
        if (command is null)
            return;

        RealServer.EnqueueCommand(command);
    }

    // ---------- PROXY DELEGATION ----------
    public new async Task Run()
    {
        await RealServer.Run();
    }
}
