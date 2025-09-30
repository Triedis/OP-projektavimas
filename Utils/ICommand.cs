using System.Text.Json.Serialization;

[JsonDerivedType(typeof(SyncCommand), typeDiscriminator: "Sync")]
[JsonDerivedType(typeof(SwingCommand), typeDiscriminator: "Swing")]
interface ICommand
{
    void ExecuteOnClient(ClientStateController gameState);
    void ExecuteOnServer(ServerStateController gameState);
}
