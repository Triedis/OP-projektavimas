using System.Text;

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
            Console.WriteLine("You must pass either --server or --client to the differentiable binary");
            throw new ArgumentException();
        }
        switch (partition)
        {
            case Partition.SERVER:
                {

                    ServerStateController server = new(port);
                    await server.Run();
                    break;
                }
                ;
            case Partition.CLIENT:
                {
                    ClientStateController client;
                    try
                    {
                        client = new(ipAddress, port);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Failed to connect to server. Is it running? {e}");
                        System.Environment.Exit(1);
                        return;
                    }
                    await client.Run();
                    break;
                }
                ;
        }
    }
}
