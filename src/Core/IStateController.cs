public interface IStateController
{
    public List<Player> players { get; set; } // Added setters
    public List<Enemy> enemies { get; set; } // Added setters
    public WorldGrid worldGrid { get; set; } // Added setters

    Task Run();
    Character? FindCharacterByIdentity(System.Guid identity);
}
