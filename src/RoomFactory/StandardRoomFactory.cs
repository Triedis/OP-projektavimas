using System;
using System.Collections.Generic;
using System.Linq;

class StandardRoomFactory : IRoomFactory
{
    public Room CreateRoom(Vector2 position, WorldGrid worldGrid)
    {
        Vector2 shape = new(
            worldGrid.random.Next(10, 25),
            worldGrid.random.Next(10, 25)
        );

        Room room = new Room(position, shape);
        Dictionary<Direction, Room?> locality = new()
        {
            { Direction.NORTH, worldGrid.GetRoom(position + DirectionUtils.GetVectorDirection(Direction.NORTH)) },
            { Direction.EAST, worldGrid.GetRoom(position + DirectionUtils.GetVectorDirection(Direction.EAST)) },
            { Direction.SOUTH, worldGrid.GetRoom(position + DirectionUtils.GetVectorDirection(Direction.SOUTH)) },
            { Direction.WEST, worldGrid.GetRoom(position + DirectionUtils.GetVectorDirection(Direction.WEST)) }
        };

        Dictionary<Direction, bool> used = new();
        int quota = 0;

        foreach (Direction dir in Enum.GetValues<Direction>())
        {
            used[dir] = false;

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
                    quota++;
                    used[dir] = true;
                }
            }
        }

        int numBoundariesToGenerate = worldGrid.random.Next(1, 5 - quota);
        for (int i = 0; i < numBoundariesToGenerate; i++)
        {
            var unusedDirections = used.Where(pair => !pair.Value).Select(pair => pair.Key).ToList();
            if (unusedDirections.Count == 0) break;

            Direction directionToUse = unusedDirections[worldGrid.random.Next(unusedDirections.Count)];

            Vector2 newBoundarypoint = directionToUse switch
            {
                Direction.NORTH => new Vector2(shape.X / 2, 0),
                Direction.EAST => new Vector2(shape.X - 1, shape.Y / 2),
                Direction.SOUTH => new Vector2(shape.X / 2, shape.Y - 1),
                Direction.WEST => new Vector2(0, shape.Y / 2),
                _ => new Vector2(shape.X / 2, shape.Y / 2),
            };
            room.BoundaryPoints[directionToUse] = new RoomBoundary(newBoundarypoint);
            used[directionToUse] = true;
        }

        return room;
    }
}
