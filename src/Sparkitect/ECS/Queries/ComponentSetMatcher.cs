using Sparkitect.Modding;

namespace Sparkitect.ECS.Queries;

/// <summary>
/// Shared matching logic for component set inclusion and exclusion requirements.
/// Eliminates duplication between keyed and non-keyed requirement variants.
/// </summary>
internal static class ComponentSetMatcher
{
    /// <summary>
    /// Returns true when the storage contains all required components.
    /// </summary>
    public static bool ContainsAll(HashSet<Identification> required, IReadOnlySet<Identification> actual)
        => required.IsSubsetOf(actual);

    /// <summary>
    /// Returns true when the storage contains none of the excluded components.
    /// </summary>
    public static bool ContainsNone(HashSet<Identification> excluded, IReadOnlySet<Identification> actual)
        => !excluded.Overlaps(actual);
}
