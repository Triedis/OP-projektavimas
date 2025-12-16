using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DungeonCrawler.src.Interpreter
{
    public interface Expression
    {
        ICommand Interpret(GameContext context);
    
    }
}
