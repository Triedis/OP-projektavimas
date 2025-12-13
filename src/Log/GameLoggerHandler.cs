abstract class GameLoggerHandler
{
    protected GameLoggerHandler? Next { get; private set; }

    public void SetNext(GameLoggerHandler next) => Next = next;

    public void Handle(LogEntry entry)
    {
        if (CanHandle(entry))
            Write(entry);

        Next?.Handle(entry);
    }

    protected abstract bool CanHandle(LogEntry entry);
    protected abstract void Write(LogEntry entry);
}
