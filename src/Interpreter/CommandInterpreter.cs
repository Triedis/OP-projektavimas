using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DungeonCrawler.src.Interpreter
{
    public class CommandInterpreter
    {
        public Expression Parse(string input)
        {
            input = input.Trim().ToLower();

            return input switch
            {
                "w" or "move w" => new MoveExpression(Direction.NORTH),
                "a" or "move a" => new MoveExpression(Direction.WEST),
                "s" or "move s" => new MoveExpression(Direction.SOUTH),
                "d" or "move d" => new MoveExpression(Direction.EAST),
                "space" or "attack" => new UseWeaponExpression(),
                _ => throw new InvalidOperationException("Unknown command")
            };
        }
    }

}
