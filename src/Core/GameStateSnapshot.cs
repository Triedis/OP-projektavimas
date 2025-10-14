class GameStateSnapshot(List<Player> players, List<Skeleton> skeletons, WorldGrid worldGrid)
{
    public List<Player> Players { get; } = players;
    public List<Skeleton> Skeletons { get; } = skeletons;
    public WorldGrid WorldGrid { get; } = worldGrid;
}
