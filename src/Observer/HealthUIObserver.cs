using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DungeonCrawler.src.Observer
{
    internal class HealthUIObserver : IHealthObserver
    {
        public void OnHealthChanged(Character character, int oldHealth, int newHealth)
        {
            Console.WriteLine($"[UI] Health changed: {oldHealth} → {newHealth}");
        }

        public void OnDeath(Character character)
        {
            Console.WriteLine("[UI] Character has died. Displaying death screen...");
        }
    }
}
