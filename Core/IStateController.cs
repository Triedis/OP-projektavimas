abstract class IStateController
{
    protected List<Player> players = new();
    protected List<Skeleton> skeletons = new();
    protected WorldGrid worldGrid = new();
    protected GameConsole console = new();
    public abstract Task Run();
}
