
public class PlayerStateCaretaker
{
    private readonly Dictionary<Guid, PlayerMemento> _mementos = new();

    public void SaveState(Player player)
    {
        _mementos[player.Identity] = player.SaveState();
    }

    public void Undo(Player player)
    {
        if (_mementos.TryGetValue(player.Identity, out var memento))
        {
            player.RestoreState(memento);
        }
    }
}
