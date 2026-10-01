namespace Content.Shared._Horizon.Husbandry.Core;

/// <summary>
/// Rules for something that is produced out of what an animal consumed (manure). Engine independent on purpose.
/// </summary>
public static class ProductionRules
{
    /// <summary>
    /// Adds <paramref name="amount"/> to <paramref name="accumulated"/> and takes out as many whole batches
    /// of <paramref name="perBatch"/> as possible.
    /// </summary>
    /// <returns>How many batches are ready.</returns>
    public static int Accumulate(ref float accumulated, float amount, float perBatch)
    {
        if (perBatch <= 0f)
            return 0;

        accumulated += amount;
        var batches = (int) MathF.Floor(accumulated / perBatch);
        accumulated -= batches * perBatch;
        return batches;
    }
}
