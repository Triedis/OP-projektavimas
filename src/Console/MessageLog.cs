using System.Collections.Specialized;

// Server-only singleton for maintaining a game log.
sealed class MessageLog
{
    private static readonly Lazy<MessageLog> lazy = new Lazy<MessageLog>(() => new MessageLog());
    public static MessageLog Instance => lazy.Value;

    private readonly List<LogEntry> _messages = [];

    public void Add(LogEntry entry)
    {
        _messages.Add(entry);
    }

    public IReadOnlyList<LogEntry> GetAllMessages()
    {
        return _messages.ToList().AsReadOnly();
    }

    private MessageLog() { }
}
