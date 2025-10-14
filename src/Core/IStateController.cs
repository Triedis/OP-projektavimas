abstract class IStateController
{
    public List<Player> players = [];
    public List<Skeleton> skeletons = [];
    public WorldGrid worldGrid = new(1337);
    public abstract Task Run();

    public Character? FindCharacterByIdentity(Guid identity)
    {
        return players.Where(player => player.Identity == identity).FirstOrDefault();
    }
}
