using System.Text.Json.Serialization;
using OP_Projektavimas.Utils;
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(Skeleton), typeDiscriminator: "Skeleton")]
[JsonDerivedType(typeof(Orc), typeDiscriminator: "Orc")]
[JsonDerivedType(typeof(Zombie), typeDiscriminator: "Zombie")]
[JsonDerivedType(typeof(Slime), typeDiscriminator: "Slime")]
[JsonDerivedType(typeof(PlayerEnemyAdapter), typeDiscriminator: "PlayerEnemyAdapter")]


public abstract class Enemy : Character, Prototype
    {
    [JsonIgnore]//deep ir shallow atributus pažiūrėt kaip klonuot
    public int attackTick = 5;
    private EnemyStrategy? strategy;
        [JsonConstructor]
        public Enemy() : base() { }
        protected Enemy(Guid identity, Room room, Vector2 positionInRoom, Weapon weapon)
            : base(room, positionInRoom, weapon, identity)
        { }
        public void SetStrategy(EnemyStrategy newStrategy)
        {
            strategy = newStrategy;
        }

        public ICommand? TickAI()
        {
            return strategy?.TickAI(this);
        }
    public virtual Enemy ShallowClone()
    {
        return (Enemy)this.MemberwiseClone();
    }

    public virtual Enemy DeepClone()
    {
        Enemy clone = (Enemy)this.MemberwiseClone();

        // Deep copy of fields that should be unique
        clone.Identity = Guid.NewGuid();
        clone.PositionInRoom = new Vector2(PositionInRoom.X, PositionInRoom.Y);

        // Make a new weapon so they don't share the same one
        clone.Weapon = new Sword(Guid.NewGuid(), Weapon.MaxRange, Weapon.Effect, Weapon.Name);

        clone.HasSplit = true;
        clone.Health = this.Health;

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