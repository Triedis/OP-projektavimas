// Authoritative non-networked commands that operate on game state.
public interface IActionCommand
{
    void Execute(IStateController gameState);
    void Undo(IStateController gameState); // as per the general Command pattern's requirement ...
    bool Expired();
    bool CanExpire();
}
