using System.Text.Json.Serialization;
using OP_Projektavimas.Utils;
[JsonDerivedType(typeof(Skeleton), typeDiscriminator: "Skeleton")]
[JsonDerivedType(typeof(Orc), typeDiscriminator: "Orc")]
[JsonDerivedType(typeof(Zombie), typeDiscriminator: "Zombie")]
[JsonDerivedType(typeof(Slime), typeDiscriminator: "Slime")]

    abstract class Enemy : Character, Prototype
    {
    [JsonIgnore]//deep ir shallow atributus pažiūrėt kaip klonuot
    public int attackTick = 5;
    private IStrategy? strategy;
        [JsonConstructor]
        public Enemy() : base() { }
        protected Enemy(Guid identity, Room room, Vector2 positionInRoom, Weapon weapon)
            : base(room, positionInRoom, weapon, identity)
        { }
        public void SetStrategy(IStrategy newStrategy)
        {
            strategy = newStrategy;
        }

        public ICommand? TickAI()
        {
            return strategy?.TickAI(this);
        }
        public Enemy Clone()
        {
            Slime clone = new Slime(Guid.NewGuid(), this.Room, this.PositionInRoom, (Sword)this.Weapon);//change this to enemy later
            return clone;
        }
        public override Character? GetClosestOpponent()
        {
            Player? nearestPlayer = null;
            double minDistance = double.MaxValue;

            foreach (Character character in Room.Occupants)
            {
                if (character is Player player && !character.Dead)
                {
                    double distance = Vector2.Distance(PositionInRoom, player.PositionInRoom);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearestPlayer = player;
                    }
                }
            }
            return nearestPlayer;
        }
    }