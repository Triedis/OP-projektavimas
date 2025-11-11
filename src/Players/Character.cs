using DungeonCrawler.src.Observer;
using Serilog;
using System.Text.Json.Serialization;

[JsonDerivedType(typeof(Player), typeDiscriminator: "Player")]
[JsonDerivedType(typeof(Skeleton), typeDiscriminator: "Skeleton")]
[JsonDerivedType(typeof(Orc), typeDiscriminator: "Orc")]
[JsonDerivedType(typeof(Zombie), typeDiscriminator: "Zombie")]
[JsonDerivedType(typeof(Slime), typeDiscriminator: "Slime")]
abstract class Character
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

    private readonly List<IHealthObserver> observers = new();

    [JsonIgnore]
    public List<IActionCommand> ActiveCommands = [];

    [JsonConstructor]
    public Character() { }
    public Character(int initialHealth)
    {
        Health = initialHealth;
        Dead = false;
    }
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

    public virtual void TakeDamage(int damage)
    {
        if (Dead) return;
        Log.Information("Character {character} takes {damage} damage.", this, damage);

        int oldHealth = Health;
        Health -= damage;
        if(Health!=oldHealth)
            NotifyHealthChanged(oldHealth, Health);

        if (Health <= 0)
        {
            Dead = true;
            Health = 0;
            NotifyDeath();
            //LogEntry characterDiedLogEntry = LogEntry.ForRoom($"{this} has died", Room);
            //MessageLog.Instance.Add(characterDiedLogEntry);

            //OnDeath?.Invoke(this); 
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
    public void Attach(IHealthObserver observer)
    {
        if (!observers.Contains(observer))
            observers.Add(observer);
    }

    public void Detach(IHealthObserver observer)
    {
        observers.Remove(observer);
    }
    private void NotifyHealthChanged(int oldHealth, int newHealth)
    {
        
        var snapshot = new List<IHealthObserver>(observers);
        foreach (var observer in snapshot)
        {
            observer.OnHealthChanged(this, oldHealth, newHealth);
        }
    }

    private void NotifyDeath()
    {
        var snapshot = new List<IHealthObserver>(observers);
        foreach (var observer in snapshot)
        {
            observer.OnDeath(this);
        }

        
        OnDeath?.Invoke(this);
    }
}
