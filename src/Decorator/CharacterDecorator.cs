using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DungeonCrawler.src.Decorator
{
    internal abstract class CharacterDecorator: Character
    {
        protected Character inner;

        protected CharacterDecorator(Character inner)
        {
            this.inner = inner;
        }

        public override void TakeDamage(int damage)
        {
            inner.TakeDamage(damage);
        }


        public override string ToString()
        {
            return inner.ToString();
        }
        public override Character? GetClosestOpponent()
        {
            return inner.GetClosestOpponent();
        }

    }
}
