using JetBrains.Annotations;

namespace Sparkitect.ECS.Queries;

/// <summary>
/// Declares the key type for keyed iteration on a component query.
/// When <see cref="Required"/> is <c>true</c>, the query only matches storages
/// implementing <c>IChunkedIteration&lt;TKey&gt;</c> (keyed iteration only).
/// When <c>false</c>, key access is opportunistic (used where available, skipped where not).
/// </summary>
/// <typeparam name="TKey">The unmanaged key type (e.g., EntityId).</typeparam>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
[PublicAPI]
public sealed class ExposeKeyAttribute<TKey> : Attribute
    where TKey : unmanaged
{
    /// <summary>
    /// Gets whether keyed iteration is required (<c>true</c>) or opportunistic (<c>false</c>).
    /// </summary>
    public bool Required { get; }

    /// <summary>Creates the attribute; <paramref name="required"/> selects required vs opportunistic keyed iteration.</summary>
    public ExposeKeyAttribute(bool required) => Required = required;
}
