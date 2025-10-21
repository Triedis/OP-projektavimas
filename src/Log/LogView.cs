using System.Numerics;

class LogView {
    private System.Guid? SubjectIdentity { get; set; }
    private Vector2? SubjectRoomPosition { get; set; }

    public void SetCharacterSubject(System.Guid subjectIdentity) {
        SubjectIdentity = subjectIdentity;
    }

    public void SetRoomSubject(Vector2 subjectRoomPosition) {
        SubjectRoomPosition = subjectRoomPosition;
    }

    public IReadOnlyList<string> GetRelevantMessages(List<LogEntryDto> allMessagesFromServer)
    {
        return allMessagesFromServer
            .Where(dto =>
                dto.Scope == LogScope.Global ||
                dto.PlayerIdentity == SubjectIdentity ||
                dto.RoomPosition == SubjectRoomPosition)
            .Select(dto => dto.Text)
            .ToList();
    }
}