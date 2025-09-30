class GameStateSnapshot
{
    public List<Player> Players { get; }
    public List<Skeleton> Skeletons { get; }
    public WorldGrid WorldGrid { get; }
    public GameConsole GameConsole { get; }

    public GameStateSnapshot(List<Player> players, List<Skeleton> skeletons, WorldGrid worldGrid, GameConsole gameConsole)
    {
        Players = players;
        Skeletons = skeletons;
        WorldGrid = worldGrid;
        GameConsole = gameConsole;
    }
}
