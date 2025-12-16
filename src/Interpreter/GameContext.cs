using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DungeonCrawler.src.Interpreter
{
    public class GameContext
    {
        public Player Player { get; }
        public WorldGrid World { get; }

        public GameContext(Player player, WorldGrid world)
        {
            Player = player;
            World = world;
        }
    }
}
