using System.Text;
using Serilog;
using Serilog.Core;

namespace DungeonCrawler;

static class DungeonCrawler
{
    private static int port = 11888;
    private static string ipAddress = "127.0.0.1";

    enum Partition
    {
        CLIENT,
        SERVER,
    }

    public static async Task Main(string[] args)
    {
        Partition partition;
        try
        {
            string partitionString = args[0][2..];
            switch (partitionString)
            {
                case "server": { partition = Partition.SERVER; break; };
                case "client": { partition = Partition.CLIENT; break; };
                default: throw new ArgumentException();
            }
        }
        catch
        {
            Log.Error("You must pass either --server or --client to the differentiable binary");
            throw;
        }
        switch (partition)
        {
            case Partition.SERVER:
                {
                    using var log = new LoggerConfiguration()
                        .MinimumLevel.Debug()
                        .WriteTo.Console()
                        .CreateLogger();
                    Log.Logger = log;

                    ServerStateController server = new ServerStateProxy(port);
                    await server.Run();
                    break;
                }
                ;
            case Partition.CLIENT:
                {
                    using var log = new LoggerConfiguration()
                        .MinimumLevel.Debug()
                        .WriteTo.File("client_log.txt", rollingInterval: RollingInterval.Minute)
                        .CreateLogger();
                    Log.Logger = log;


                    ClientStateController client;
                    try
                    {
                        client = await ClientStateController.CreateAsync(ipAddress, port);
                    }
                    catch (Exception e)
                    {
                        Log.Error("Failed to connect to server. Is it running? {e}", e);
                        throw;
                    }
                    await client.Run();
                    break;
                }
                ;
        }
    }
}
