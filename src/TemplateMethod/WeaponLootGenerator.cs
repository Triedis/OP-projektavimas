using System;

namespace TemplateMethod;

public sealed class WeaponLootGenerator : LootGenerator
{
    protected override LootDrop CreateLootDrop(Vector2 position)
    {
        Weapon newWeapon = new Sword(Guid.NewGuid(), 1, new PhysicalDamageEffect(5), "Rusty Sword");
        return new WeaponLootDrop(newWeapon, position);
    }

    protected override bool ShouldDropLoot()
    {
        return Random.Shared.NextDouble() < 0.5;
    }
}
