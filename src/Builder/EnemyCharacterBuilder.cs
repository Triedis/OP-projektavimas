using DungeonCrawler.src.Observer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DungeonCrawler.src.Builder
{
    internal class EnemyCharacterBuilder : ICharacterBuilder
    {
        private Character _character;
        private float _difficultyMultiplier = 1.0f;

        public void Reset()
        {
            _character = new Skeleton(); // or a generic enemy type
        }

        public void SetHealth(int health)
        {
            _character.Health = (int)(health * _difficultyMultiplier);
        }

        public void SetPosition(Vector2 position)
        {
            _character.PositionInRoom = position;
        }

        public void SetHasBlood(bool hasBlood)
        {
            // Apply property if enemy type supports it
        }

        public void AddObserver(IHealthObserver observer)
        {
            _character.Attach(observer);
        }

        public Character Build()
        {
            var result = _character;
            Reset();
            return result;
        }

        public EnemyCharacterBuilder WithDifficulty(float difficulty)
        {
            _difficultyMultiplier = difficulty;
            return this;
        }
    }
}
