using System;

namespace TemplateMethod;

public abstract class LootGenerator
{
    public LootDrop? GenerateLoot(Vector2 position)
    {
        if (!ShouldDropLoot())
        {
            return null;
        }

        LootDrop drop = CreateLootDrop(position);
        ConfigureLoot(drop);
        return drop;
    }

    protected virtual bool ShouldDropLoot()
    {
        return Random.Shared.NextDouble() < 0.5;
    }

    protected abstract LootDrop CreateLootDrop(Vector2 position);

    protected virtual void ConfigureLoot(LootDrop drop)
    {
        // Optional hook
    }
}
