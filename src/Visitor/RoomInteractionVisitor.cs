// A visitor that performs a specific interaction based on the room type.
public class RoomInteractionVisitor : IRoomVisitor
{
    private readonly Player _player;

    public RoomInteractionVisitor(Player player)
    {
        _player = player;
    }

    public void Visit(StandardRoom room)
    {
        bool foundLoot = CollectFloorLoot(room);
        if (!foundLoot)
        {
            MessageLog.Instance.Add(new LogEntry(Loggers.Game, "You see nothing of interest to interact with here."));
        }
    }

    public void Visit(TreasureRoom room)
    {
        CollectFloorLoot(room);

        if (room.IsLooted)
        {
            MessageLog.Instance.Add(new LogEntry(Loggers.Game, "The treasure chest is empty."));
            return;
        }

        if (room.Occupants.OfType<Enemy>().Any(e => !e.Dead))
        {
            MessageLog.Instance.Add(new LogEntry(Loggers.Game, "You must defeat the enemies before you can loot the treasure."));
            return;
        }
        
        if (room.Loot != null)
        {
            var lootMessage = room.Loot.Collect(_player);
            room.IsLooted = true;
            MessageLog.Instance.Add(new LogEntry(Loggers.Game, lootMessage));
        }
        else
        {
            MessageLog.Instance.Add(new LogEntry(Loggers.Game, "You find a treasure chest, but it's mysteriously empty."));
        }
    }

    public void Visit(BossRoom room)
    {
        CollectFloorLoot(room);

        if (room.IsBossDefeated)
        {
            MessageLog.Instance.Add(new LogEntry(Loggers.Game, "The boss has been defeated. Its corpse lies here."));
        }
        else
        {
            MessageLog.Instance.Add(new LogEntry(Loggers.Game, "The boss is still alive! Prepare for battle!"));
        }
    }

    private bool CollectFloorLoot(Room room)
    {
        var lootAtPosition = room.LootDrops
            .Where(l => l.PositionInRoom.Equals(_player.PositionInRoom))
            .ToList();

        if (lootAtPosition.Count == 0)
        {
            return false;
        }

        foreach (var loot in lootAtPosition)
        {
            string message = loot.Collect(_player);
            MessageLog.Instance.Add(new LogEntry(Loggers.Game, message));
            room.LootDrops.Remove(loot);
        }

        return true;
    }
}
