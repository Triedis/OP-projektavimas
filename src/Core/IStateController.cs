abstract class IStateController
{
    public List<Player> players = new();
    public List<Skeleton> skeletons = new();
    public WorldGrid worldGrid = new();
    public GameConsole console = new();
    public abstract Task Run();
}
