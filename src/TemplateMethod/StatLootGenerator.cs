using System;

namespace TemplateMethod;

public sealed class StatLootGenerator : LootGenerator
{
    protected override LootDrop CreateLootDrop(Vector2 position)
    {
        return new StatLootDrop(10, position);
    }
}
