using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DungeonCrawler.src.Decorator
{
    internal class WeaponDecorator : CharacterDecorator
    {
        private readonly int attackBonus;

        public WeaponDecorator(Character inner, int attackBonus = 10)
            : base(inner)
        {
            this.attackBonus = attackBonus;
        }

        public override void TakeDamage(int damage)
        {
            Log.Information("WeaponDecorator on {character} does not modify damage directly.", inner);

            LogEntry log = LogEntry.ForRoom(
                $"{inner}'s weapon glows faintly but offers no protection this time.",
                inner.Room
            );
            MessageLog.Instance.Add(log);

            base.TakeDamage(damage);
        }
    }

}
