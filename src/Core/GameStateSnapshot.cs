class GameStateSnapshot(List<Player> players, List<Enemy> enemies, WorldGrid worldGrid, List<LogEntryDto> logEntries)
{
    public List<Player> Players { get; } = players;
    public List<Enemy> Enemies { get; } = enemies;
    public WorldGrid WorldGrid { get; } = worldGrid;
    public List<LogEntryDto> LogEntries { get; } = logEntries;
}
