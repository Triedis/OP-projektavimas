using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DungeonCrawler.src.Iterators
{
    public interface IterableCollection<T>
    {
        Iterator<T> CreateIterator();
    }
}
