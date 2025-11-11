using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DungeonCrawler.src.Builder
{
    internal class CharacterDirector
    {
        private ICharacterBuilder _builder;

        public void SetBuilder(ICharacterBuilder builder)
        {
            _builder = builder;
        }

        public Character ConstructPlayer(Vector2 startPosition)
        {
            _builder.Reset();
            _builder.SetPosition(startPosition);
            _builder.SetHealth(100);
            return _builder.Build();
        }

        public Character ConstructEnemy(Vector2 startPosition, float difficulty)
        {
            if (_builder is EnemyCharacterBuilder enemyBuilder)
            {
                enemyBuilder.WithDifficulty(difficulty);
            }

            _builder.Reset();
            _builder.SetPosition(startPosition);
            _builder.SetHealth(50);
            return _builder.Build();
        }
    }
}
