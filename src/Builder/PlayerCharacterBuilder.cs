using DungeonCrawler.src.Observer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DungeonCrawler.src.Builder
{
    internal class PlayerCharacterBuilder : ICharacterBuilder
    {
        private Player _player;

        private string _name = "Hero";
        private Color _color ;
        private Room _room;
        private Vector2 _position;
        private Weapon _weapon;
        private Guid _identity = Guid.NewGuid();

        public void Reset()
        {
            _player = null;
        }

        public void SetRoom(Room room)
        {
            _room = room;
        }

        public void SetPosition(Vector2 position)
        {
            _position = position;
        }

        public void SetWeapon(Weapon weapon)
        {
            _weapon = weapon;
        }

        public void SetName(string name)
        {
            _name = name;
        }

        public void SetColor(Color color)
        {
            _color = color;
        }

        public void SetHealth(int health)
        {
            if (_player != null) _player.Health = health;
        }

        public void SetHasBlood(bool hasBlood)
        {
            // optional
        }

        public void AddObserver(IHealthObserver observer)
        {
            _player?.Attach(observer);
        }

        public Character Build()
        {
            _player = new Player(_name, _identity, _color, _room, _position, _weapon);
            return _player;
        }
    }
}
