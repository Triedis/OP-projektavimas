using System.Diagnostics;
using Serilog;

class UseWeaponCommand(Guid actorIdentity) : ICommand
{
    public Guid ActorIdentity { get; } = actorIdentity;

    public async Task ExecuteOnClient(ClientStateController gameState)
    {
        await gameState.SendCommand(this);
    }

    public Task ExecuteOnServer(ServerStateController gameState)
    {
        Log.Information("UseWeaponCommand::ExecuteOnServer from {actorIdentity}", ActorIdentity);

        Character? actor = gameState.players.Where(player => player.Identity == ActorIdentity).FirstOrDefault();
        if (actor is null) {
            Log.Warning("UseWeaponCommand's ActorIdentity is not bound to any character object");
            return Task.CompletedTask;
        }
        Weapon weapon = actor.Weapon;

        Room room = actor.Room;
        Character? target = actor.GetClosestOpponent();
        if (target is null) {
            Log.Debug("UseWeaponCommand has no suitable target");
            return Task.CompletedTask;
        }

        if (weapon.CanUse(actor, target, gameState))
        {
            Log.Information("Weapon acting on {tgt} {id}", target, target.Identity);
            LogEntry weaponUseLogEntry = LogEntry.ForRoom($"{actor} swings and hits {target}", room);
            MessageLog.Instance.Add(weaponUseLogEntry);
            
            IReadOnlyList<IActionCommand> consequencues = weapon.Act(actor, target);
            foreach (IActionCommand consequence in consequencues) {
                consequence.Execute(gameState);
            }
        }

        return Task.CompletedTask;
    }
}