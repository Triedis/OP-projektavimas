using System.Security.Principal;
using System.Text.Json.Serialization;

[JsonDerivedType(typeof(Player), typeDiscriminator: "Player")]
[JsonDerivedType(typeof(Skeleton), typeDiscriminator: "Skeleton")]


abstract class Character(Room room, Vector2 positionInRoom, Sword weapon, string identity)
{
    public string identity = identity;
    public Vector2 PositionInRoom { get; protected set; } = positionInRoom;
    public int Health { get; protected set; } = 100;
    public Sword Weapon { get; protected set; } = weapon;
    public Boolean Dead { get; protected set; } = false;
    public Room Room { get; protected set;} = room;

    public void EnterRoom(Room room) {
        Room = room;
    }

    public void SetPositionInRoom(Vector2 position) {
        PositionInRoom = position;
    }


    public override bool Equals(object? obj)
    {
        return obj is Character character &&
               identity == character.identity;
    }
}
