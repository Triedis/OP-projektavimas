using DungeonCrawler.src.Observer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DungeonCrawler.src.Builder
{
    internal interface ICharacterBuilder
    {
        void Reset();
        void SetHealth(int health);
        void SetPosition(Vector2 position);
        void SetHasBlood(bool hasBlood);
        void AddObserver(IHealthObserver observer);
        Character Build();
    }
}
