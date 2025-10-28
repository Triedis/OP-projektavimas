using System.Text.Json.Serialization;

[JsonDerivedType(typeof(SyncCommand), typeDiscriminator: "Sync")]
[JsonDerivedType(typeof(UseWeaponCommand), typeDiscriminator: "UseWeapon")]
[JsonDerivedType(typeof(MoveCommand), typeDiscriminator: "Move")]
[JsonDerivedType(typeof(SpawnEnemyCommand), typeDiscriminator: "Spawn")]

interface ICommand
{
    Task ExecuteOnClient(ClientStateController gameState);
    Task ExecuteOnServer(ServerStateController gameState);
}
