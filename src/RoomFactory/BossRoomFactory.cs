using System;
using System.Collections.Generic;
using System.Linq;

class BossRoomFactory : IRoomFactory
{
    public Room CreateRoom(Vector2 position, WorldGrid worldGrid)
    {
        Vector2 shape = new(30, 30);

        Room room = new Room(position, shape);

        Dictionary<Direction, Room?> locality = new()
        {
            { Direction.NORTH, worldGrid.GetRoom(position + DirectionUtils.GetVectorDirection(Direction.NORTH)) },
            { Direction.EAST, worldGrid.GetRoom(position + DirectionUtils.GetVectorDirection(Direction.EAST)) },
            { Direction.SOUTH, worldGrid.GetRoom(position + DirectionUtils.GetVectorDirection(Direction.SOUTH)) },
            { Direction.WEST, worldGrid.GetRoom(position + DirectionUtils.GetVectorDirection(Direction.WEST)) }
        };

        // Find an adjacent room to connect to, only create one entrance.
        foreach (Direction dir in Enum.GetValues<Direction>())
        {
            if (locality.TryGetValue(dir, out Room? adjacent) && adjacent != null)
            {
                if (adjacent.BoundaryPoints.ContainsKey(DirectionUtils.GetOpposite(dir)))
                {
                    Vector2 newBoundarypoint = dir switch
                    {
                        Direction.NORTH => new Vector2(shape.X / 2, 0),
                        Direction.EAST => new Vector2(shape.X - 1, shape.Y / 2),
                        Direction.SOUTH => new Vector2(shape.X / 2, shape.Y - 1),
                        Direction.WEST => new Vector2(0, shape.Y / 2),
                        _ => new Vector2(shape.X / 2, shape.Y / 2),
                    };
                    room.BoundaryPoints[dir] = new RoomBoundary(newBoundarypoint);
                    break;
                }
            }
        }

        // If no adjacent rooms were found to connect to, add a default
        if (room.BoundaryPoints.Count == 0)
        {
            // Default to a south entrance
            room.BoundaryPoints[Direction.SOUTH] = new RoomBoundary(new Vector2(shape.X / 2, shape.Y - 1));
        }


        // TODO: Add a boss enemy to this room.
        // For example: room.Occupants.Add(EnemyFactory.CreateBoss());

        return room;
    }
}
