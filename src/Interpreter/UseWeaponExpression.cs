using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DungeonCrawler.src.Interpreter
{
    public class UseWeaponExpression : Expression
    {
        public ICommand Interpret(GameContext context)
        {
            return new UseWeaponCommand(context.Player.Identity);
        }
    }
}
