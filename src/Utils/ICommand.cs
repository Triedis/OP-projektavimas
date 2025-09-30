using System.Text.Json.Serialization;

[JsonDerivedType(typeof(SyncCommand), typeDiscriminator: "Sync")]
[JsonDerivedType(typeof(SwingCommand), typeDiscriminator: "Swing")]
[JsonDerivedType(typeof(MoveCommand), typeDiscriminator: "Move")]

interface ICommand
{
    Task ExecuteOnClient(ClientStateController gameState);
    Task ExecuteOnServer(ServerStateController gameState);
}
