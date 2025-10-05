using System;

namespace Mountain;

public static class SceneTraceExtensions
{
    public static HitboxTags GetHitboxTags(this SceneTraceResult tr)
    {
        if (tr.Hitbox is null)
        {
            return HitboxTags.None;
        }

        var tags = HitboxTags.None;

        foreach (var tag in tr.Hitbox.Tags)
        {
            if (Enum.TryParse<HitboxTags>(tag, true, out var hitboxTag))
            {
                tags |= hitboxTag;
            }
        }

        return tags;
    }
}