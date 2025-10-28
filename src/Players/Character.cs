using System.Text.Json.Serialization;

[JsonDerivedType(typeof(Player), typeDiscriminator: "Player")]
[JsonDerivedType(typeof(Skeleton), typeDiscriminator: "Skeleton")]
[JsonDerivedType(typeof(Orc), typeDiscriminator: "Orc")]
[JsonDerivedType(typeof(Zombie), typeDiscriminator: "Zombie")]
abstract class Character
{
    public Guid Identity { get; set; }
    public Vector2 PositionInRoom { get; set; }
    public int Health { get; set; } = 100;
    public Weapon Weapon { get; set; }
    public bool Dead { get; set; } = false;
    public Room Room { get; set; }

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
        }
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
