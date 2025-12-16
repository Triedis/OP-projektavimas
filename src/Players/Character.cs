using System.Text.Json.Serialization;
using TemplateMethod;

[JsonDerivedType(typeof(Player), typeDiscriminator: "Player")]
[JsonDerivedType(typeof(Skeleton), typeDiscriminator: "Skeleton")]
[JsonDerivedType(typeof(Orc), typeDiscriminator: "Orc")]
[JsonDerivedType(typeof(Zombie), typeDiscriminator: "Zombie")]
[JsonDerivedType(typeof(Slime), typeDiscriminator: "Slime")]
public abstract class Character
{
    public Guid Identity { get; set; }
    public Vector2 PositionInRoom { get; set; }
    public int StartingHealth { get; set; } = 100;
    public bool HasSplit { get; set; } = false;
    public int Health { get; set; } = 100;
    public virtual Weapon Weapon { get; set; }
    public virtual bool Dead { get; set; } = false;
    public virtual Room Room { get; set; }
    public event Action<Character> OnDeath; // Event-driven death notification

    [JsonIgnore]
    public List<IActionCommand> ActiveCommands = [];

    [JsonConstructor]
    public Character() { }

    protected Character(Room room, Vector2 positionInRoom, Weapon weapon, Guid identity)
    {
        Room = room;
        PositionInRoom = positionInRoom;
        Weapon = weapon;
        Identity = identity;
    }


    public void EnterRoom(Room room)
    {
        Room = room;
    }

    public void SetPositionInRoom(Vector2 position)
    {
        PositionInRoom = position;
    }

    public void TakeDamage(int damage)
    {
        if (Dead) return;

        Health -= damage;
        if (Health <= 0)
        {
            Dead = true;
            Health = 0;

            LogEntry characterDiedLogEntry = LogEntry.ForRoom($"{this} has died", Room);
            MessageLog.Instance.Add(characterDiedLogEntry);

            if (this is Enemy)
            {
                var rng = new Random();

                // Refactored to use Template Method Pattern (LootGenerator)
                LootGenerator generator;
                double globalMagnitude = Vector2.Distance(new Vector2(0, 0), this.Room.WorldGridPosition);
                if (rng.NextDouble() < 0.1 * globalMagnitude)
                {
                    generator = new WeaponLootGenerator();
                }
                else
                {
                    generator = new StatLootGenerator();
                }

                LootDrop? drop = generator.GenerateLoot(PositionInRoom);

                if (drop != null)
                {
                    Room.LootDrops.Add(drop);
                    MessageLog.Instance.Add(LogEntry.ForRoom($"Something dropped at {PositionInRoom}!", Room));
                }
            }

            OnDeath?.Invoke(this);
        }
    }

    public void Heal(int points)
    {
        if (Dead) return;

        Health += points;
    }

    /// <summary>
    /// Returns the closest hostile entity.
    /// </summary>
    /// <returns></returns>
    public abstract Character? GetClosestOpponent();

    public void Destroy()
    {
        Room.Exit(this);
        Room.Occupants.Remove(this);
    }

    public int GetDistanceTo(Character target)
    {
        if (!Room.Equals(target.Room))
        {
            return int.MaxValue;
        }

        return (int)Vector2.Distance(PositionInRoom, target.PositionInRoom);

    }

    public override bool Equals(object? obj)
    {
        return obj is Character character &&
               Identity == character.Identity;
    }

    public override int GetHashCode()
    {
        return Identity.GetHashCode();
    }

    public override string? ToString()
    {
        return $"char:{Identity}";
    }
}
