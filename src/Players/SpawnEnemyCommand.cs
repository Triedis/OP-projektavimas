using Serilog;

class SpawnEnemyCommand(Enemy enemy) : ICommand
{
    private Enemy EnemyToSpawn { get; } = enemy;

    public async Task ExecuteOnClient(ClientStateController gameState)
    {
        await gameState.SendCommand(this);
    }

    public Task ExecuteOnServer(ServerStateController gameState)
    {
        Log.Information("Spawning enemy {enemy} in room {room}", EnemyToSpawn, EnemyToSpawn.Room);

        // Instead of directly adding to enemies, enqueue it for the next tick
        if(gameState is ServerStateController)
        {
            gameState.EnqueueEnemySpawn(EnemyToSpawn);
        }
            

        Log.Information("Enemy spawn enqueued");
        return Task.CompletedTask;
    }
}
