using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DungeonCrawler.src.Decorator
{
    internal class SpeedBuffDecorator : CharacterDecorator
    {
        private readonly float speedMultiplier;

        public SpeedBuffDecorator(Character inner, float speedMultiplier = 1.5f)
            : base(inner)
        {
            this.speedMultiplier = speedMultiplier;
        }

        public override void TakeDamage(int damage)
        {
            Log.Information("SpeedBuffDecorator active on {character}. Incoming damage: {damage}", inner, damage);

            // faster characters take slightly less damage
            int reducedDamage = (int)(damage * 0.9);

            Log.Information("Speed reduced damage by 10%. Final damage: {finalDamage}", reducedDamage);

            LogEntry log = LogEntry.ForRoom(
                $"{inner} evades partially due to speed buff (damage reduced by 10%).",
                inner.Room
            );
            MessageLog.Instance.Add(log);

            base.TakeDamage(reducedDamage);
        }
    }

}
