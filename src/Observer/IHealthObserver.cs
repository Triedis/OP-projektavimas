using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DungeonCrawler.src.Observer
{
    internal interface IHealthObserver
    {
        public void OnHealthChanged(Character character, int oldHealth, int newHealth);
        public void OnDeath(Character character);
    }
}
