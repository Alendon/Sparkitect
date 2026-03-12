using Sparkitect.Modding;

namespace Sparkitect.ECS.Capabilities;

/// <summary>
/// Capability interface for typed component access, generic on key type.
/// </summary>
/// <typeparam name="TKey">The unmanaged entity key type.</typeparam>
public interface IComponentAccess<TKey> : ICapability
    where TKey : unmanaged
{
    /// <summary>
    /// Returns a reference to the component value at the given slot.
    /// </summary>
    /// <typeparam name="T">The unmanaged component type.</typeparam>
    /// <param name="componentId">The component identification to locate the correct column.</param>
    /// <param name="slot">The slot index within the storage.</param>
    /// <returns>A reference to the component value.</returns>
    ref T Get<T>(Identification componentId, TKey slot) where T : unmanaged;

    /// <summary>
    /// Sets the component value at the given slot.
    /// </summary>
    /// <typeparam name="T">The unmanaged component type.</typeparam>
    /// <param name="componentId">The component identification to locate the correct column.</param>
    /// <param name="slot">The slot index within the storage.</param>
    /// <param name="value">The value to store.</param>
    void Set<T>(Identification componentId, TKey slot, T value) where T : unmanaged;
}
