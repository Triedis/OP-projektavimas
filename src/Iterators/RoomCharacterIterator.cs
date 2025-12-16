using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DungeonCrawler.src.Iterators
{
    public class RoomCharacterIterator : Iterator<Character>
    {
        private readonly List<Character> _characters;
        private int _index = 0;

        public RoomCharacterIterator(List<Character> characters)
        {
            _characters = characters;
        }

        public bool HasNext()
        {
            return _index < _characters.Count;
        }

        public Character Next()
        {
            if (!HasNext())
                throw new InvalidOperationException("No more characters");

            return _characters[_index++];
        }
    }
}
