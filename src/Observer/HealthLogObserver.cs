using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DungeonCrawler.src.Observer
{
    internal class HealthLogObserver : IHealthObserver
    {
        public void OnHealthChanged(Character character, int oldHealth, int newHealth)
        {
            Log.Information(
                "Health changed for {character}: {old} → {new}",
                character,
                oldHealth,
                newHealth
            );

            LogEntry healthChangeEntry = LogEntry.ForRoom(
                $"{character} health changed from {oldHealth} to {newHealth}.",
                character.Room
            );
            MessageLog.Instance.Add(healthChangeEntry);
        }

        public void OnDeath(Character character)
        {
            Log.Information("Character {character} has died.", character);

            LogEntry deathLogEntry = LogEntry.ForRoom(
                $"{character} has died.",
                character.Room
            );
            MessageLog.Instance.Add(deathLogEntry);
        }
    }
}
