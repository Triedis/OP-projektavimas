class GlobalLogger : GameLoggerHandler
{
    protected override bool CanHandle(LogEntry entry) => entry.Scope == LogScope.Global;
    protected override void Write(LogEntry entry) => Console.WriteLine($"[GLOBAL] {entry.Text}");
}

class PlayerLogger : GameLoggerHandler
{
    private readonly System.Guid _subjectId;

    public PlayerLogger(System.Guid subjectId) => _subjectId = subjectId;

    protected override bool CanHandle(LogEntry entry) =>
    entry.Scope == LogScope.Player && entry.PlayerIdentity?.Identity == _subjectId;

    protected override void Write(LogEntry entry) => Console.WriteLine($"[PLAYER {_subjectId}] {entry.Text}");
}

class RoomLogger : GameLoggerHandler
{
    private readonly Vector2 _subjectRoom;

    public RoomLogger(Vector2 roomPos) => _subjectRoom = roomPos;

    protected override bool CanHandle(LogEntry entry) => entry.Scope == LogScope.Room && entry.RoomPosition == _subjectRoom;
    protected override void Write(LogEntry entry) => Console.WriteLine($"[ROOM {_subjectRoom}] {entry.Text}");
}

class AllLogger : GameLoggerHandler
{
    protected override bool CanHandle(LogEntry entry) => true; // catches everything
    protected override void Write(LogEntry entry) => Console.WriteLine($"[ALL] {entry.Text}");
}
