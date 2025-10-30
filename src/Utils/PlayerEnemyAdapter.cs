using System;
using OP_Projektavimas.Utils;
using System.Text.Json.Serialization;

class PlayerEnemyAdapter : Enemy
{
	[JsonIgnore]
	private readonly Player _player;

	public string PlayerName => _player.Username;
	public Vector2 ClientPosition => _player.PositionInRoom;
	public Color Color => _player.Color;

	// Override serialization-sensitive properties
	[JsonIgnore] public override Room Room => base.Room;
	[JsonIgnore] public override Weapon Weapon => base.Weapon;

	public PlayerEnemyAdapter(Player player, Room room)
		: base(Guid.NewGuid(), room, player.PositionInRoom, new Dagger(Guid.NewGuid(), player.Weapon.MaxRange, new PhysicalDamageEffect(player.Weapon.Effect.Power)))
	{
		_player = player;
		StartingHealth = player.Health;
		HasSplit = false;
		SetStrategy(new ShallowSplitStrategy()); // or any default strategy
	}

	public override string ToString()
	{
		return $"PlayerEnemyAdapter({_player.Username})";
	}

	// Optional override if you want it to behave slightly differently
	public override Character? GetClosestOpponent()
	{
		// Let�s reuse player�s opponent logic
		return _player.GetClosestOpponent();
	}

	// Adapter can also delegate to player's attributes if needed
	public override bool Dead => _player.Dead;
}
