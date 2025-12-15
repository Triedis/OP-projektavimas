public struct LogEntryDto(string text, LogScope scope, System.Guid? playerIdentity, Vector2? roomPosition) {
    public string Text { get; set; } = text;
    public LogScope Scope { get; set; } = scope;
    public System.Guid? PlayerIdentity { get; set; } = playerIdentity;
    public Vector2? RoomPosition { get; set; } = roomPosition;
}