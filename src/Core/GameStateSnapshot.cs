class GameStateSnapshot(List<Player> players, List<Skeleton> skeletons, WorldGrid worldGrid, List<LogEntryDto> logEntries)
{
    public List<Player> Players { get; } = players;
    public List<Skeleton> Skeletons { get; } = skeletons;
    public WorldGrid WorldGrid { get; } = worldGrid;
    public List<LogEntryDto> LogEntries { get; } = logEntries;
}
