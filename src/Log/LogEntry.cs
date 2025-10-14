enum LogScope
{
    Player,
    Room,
    Global,
}

struct LogEntry
{
    public string Text { get; }
    public LogScope Scope { get; }

    public Guid? PlayerIdentity { get; }
    public Vector2? RoomPosition { get; }

    private LogEntry(string text, LogScope scope, Guid? playerIdentity, Vector2? roomPosition)
    {
        Text = text;
        Scope = scope;
        PlayerIdentity = playerIdentity;
        RoomPosition = roomPosition;
    }

    public static LogEntry ForGlobal(string text)
    {
        return new(text, LogScope.Global, null, null);
    }

    public static LogEntry ForPlayer(string text, Player player)
    {
        return new(text, LogScope.Player, player.Identity, null);
    }

    public static LogEntry ForRoom(string text, Room room)
    {
        return new(text, LogScope.Room, null, room.WorldGridPosition);
    }
}
