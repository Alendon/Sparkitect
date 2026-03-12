using Sparkitect.Modding;

namespace Sparkitect.ECS.Components;

/// <summary>
/// Stores component metadata (size) per component <see cref="Identification"/>.
/// Used by storages to manage raw NativeMemory allocations without needing the CLR type.
/// </summary>
public interface IComponentManager
{
    /// <summary>
    /// Registers an unmanaged component type, storing its size metadata.
    /// </summary>
    /// <typeparam name="T">The unmanaged component type.</typeparam>
    /// <param name="id">The component identification.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="id"/> is already registered.</exception>
    void Register<T>(Identification id) where T : unmanaged;

    /// <summary>
    /// Retrieves the stored size for a component.
    /// </summary>
    /// <param name="id">The component identification.</param>
    /// <returns>The size in bytes.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if <paramref name="id"/> is not registered.</exception>
    int GetSize(Identification id);

    /// <summary>
    /// Checks whether a component is registered.
    /// </summary>
    /// <param name="id">The component identification.</param>
    /// <returns><c>true</c> if the component is known; otherwise <c>false</c>.</returns>
    bool IsRegistered(Identification id);
}
