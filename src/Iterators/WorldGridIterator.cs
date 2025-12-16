using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DungeonCrawler.src.Iterators
{
    public class WorldGridIterator : Iterator<Room>
    {
        private readonly List<Room> _rooms;
        private int _index = 0;

        public WorldGridIterator(List<Room> rooms)
        {
            _rooms = rooms;
        }

        public bool HasNext()
        {
            return _index < _rooms.Count;
        }

        public Room Next()
        {
            if (!HasNext())
                throw new InvalidOperationException("No more rooms");

            return _rooms[_index++];
        }
    }
}
