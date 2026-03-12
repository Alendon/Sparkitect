using Sparkitect.Modding;

namespace Sparkitect.ECS.Components;

/// <summary>
/// Registry for unmanaged component types. Components are registered with
/// [UnmanagedComponentRegistry.RegisterComponent("key")] attribute on component structs.
/// Delegates to <see cref="IComponentManager"/>.
/// </summary>
[Registry(Identifier = "unmanaged_component")]
public partial class UnmanagedComponentRegistry(IComponentManager componentManager) : IRegistry
{
    /// <inheritdoc/>
    public static string Identifier => "unmanaged_component";

    /// <summary>
    /// Registers an unmanaged component type.
    /// </summary>
    /// <typeparam name="T">The unmanaged component type implementing <see cref="IHasIdentification"/>.</typeparam>
    /// <param name="id">The component identification.</param>
    [RegistryMethod]
    public void RegisterComponent<T>(Identification id) where T : unmanaged, IHasIdentification
    {
        componentManager.Register<T>(id);
    }

    /// <inheritdoc/>
    public void Unregister(Identification id)
    {
        // Component metadata removal is not yet needed at runtime.
    }
}
