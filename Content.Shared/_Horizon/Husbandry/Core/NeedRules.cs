namespace Content.Shared._Horizon.Husbandry.Core;

/// <summary>
/// Rules for a need that drains over time (satiety, hydration). Engine independent on purpose.
/// </summary>
public static class NeedRules
{
    /// <summary>
    /// Drains <paramref name="value"/> for the given time, never below zero.
    /// </summary>
    /// <returns>For how many minutes the need was empty during this step, used to scale the deprivation damage.</returns>
    public static float Tick(ref float value, float decayPerMinute, float seconds)
    {
        value = MathF.Max(0f, value - decayPerMinute * seconds / 60f);
        return value <= 0f ? seconds / 60f : 0f;
    }

    /// <summary>
    /// Adds <paramref name="amount"/> to <paramref name="value"/>, clamped to <c>[0, max]</c>.
    /// </summary>
    /// <returns>The amount that actually changed the value.</returns>
    public static float Modify(ref float value, float max, float amount)
    {
        var before = value;
        value = Math.Clamp(value + amount, 0f, max);
        return value - before;
    }
}
