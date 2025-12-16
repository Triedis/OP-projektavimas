


using Serilog;

class PlayingState : IClientState
{
    public void Enter(ClientStateController clinet)
    {
        Log.Information("PLAYER STATE: playing");
    }

    public async Task HandleInput(ClientStateController client, ConsoleKey key)
    {
        if (client.Identity == null)
            return;

        Vector2? moveDirection = null;
        bool shouldUseWeapon = false;

        // Map keys to actions
        switch (key)
        {
            case ConsoleKey.W:
                moveDirection = new Vector2(0, -1);
                break;
            case ConsoleKey.S:
                moveDirection = new Vector2(0, 1);
                break;
            case ConsoleKey.A:
                moveDirection = new Vector2(-1, 0);
                break;
            case ConsoleKey.D:
                moveDirection = new Vector2(1, 0);
                break;
            case ConsoleKey.Spacebar:
                shouldUseWeapon = true;
                break;
            case ConsoleKey.E: // Enemy count visitor
                var enemyCountVisitor = new EnemyCountVisitor();
                client.worldGrid.Accept(enemyCountVisitor);
                MessageLog.Instance.Add(new LogEntry(Loggers.Game, enemyCountVisitor.GetReport()));
                break;
            case ConsoleKey.I: // Room interaction visitor
                if (client.Identity.Room is IVisitableRoom interactableRoom)
                {
                    var roomInteractionVisitor = new RoomInteractionVisitor(client.Identity);
                    interactableRoom.Accept(roomInteractionVisitor);
                }
                break;
        }
        if (moveDirection is not null)
        {
            Vector2 newPos = client.Identity.PositionInRoom + moveDirection;
            var moveCommand = new MoveCommand(newPos, client.Identity.Identity);
            await client.Mediator.NotifyServerCommand(moveCommand);
        }

        // Execute weapon use
        if (shouldUseWeapon)
        {
            var weaponCommand = new UseWeaponCommand(client.Identity.Identity);
            await client.Mediator.NotifyServerCommand(weaponCommand);
        }
    }

    public Task Update(ClientStateController client)
    {
        if(client.Identity != null && client.Identity.Dead)
        {
            client.Mediator.ChangeState(new DeadState());
        }
        return Task.CompletedTask;
    }
    public void Exit(ClientStateController client)
    {
        
    }
}