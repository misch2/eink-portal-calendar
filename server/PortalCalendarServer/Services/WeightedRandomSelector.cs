namespace PortalCalendarServer.Services;

/// <summary>
/// Provides weighted random selection from a list of items.
/// </summary>
public static class WeightedRandomSelector
{
    /// <summary>
    /// Selects an item from <paramref name="items"/> using weighted random selection.
    /// Items with zero or negative weights are excluded from selection.
    /// Returns null if no valid candidates exist.
    /// </summary>
    public static T? Select<T>(IReadOnlyList<T> items, Func<T, double> weightSelector, Random random) where T : class
    {
        if (items.Count == 0)
            return null;

        var weighted = items.Where(i => weightSelector(i) > 0).ToList();
        if (weighted.Count == 0)
            return null;

        var totalWeight = weighted.Sum(weightSelector);
        var roll = random.NextDouble() * totalWeight;
        var cumulative = 0.0;

        foreach (var item in weighted)
        {
            cumulative += weightSelector(item);
            if (roll < cumulative)
                return item;
        }

        return weighted[^1]; // fallback for floating-point edge case
    }
}
