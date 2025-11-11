using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DungeonCrawler.src.Decorator
{
    internal class ArmorDecorator : CharacterDecorator
    {
        private readonly int damageReduction;

        public ArmorDecorator(Character inner, int damageReduction = 5)
            : base(inner)
        {
            this.damageReduction = damageReduction;
        }

        public override void TakeDamage(int damage)
        {
            Log.Information("ArmorDecorator active on {character}. Incoming damage: {damage}", inner, damage);

            int reducedDamage = Math.Max(0, damage - damageReduction);

            Log.Information("Damage reduced by {reduction}. Final damage: {finalDamage}", damageReduction, reducedDamage);

            LogEntry log = LogEntry.ForRoom(
                $"{inner} blocked {damageReduction} damage with armor.",
                inner.Room
            );
            MessageLog.Instance.Add(log);

            base.TakeDamage(reducedDamage);
        }
    }

}
