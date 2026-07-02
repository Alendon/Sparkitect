using JetBrains.Annotations;
using Sparkitect.GameState;

namespace Sparkitect.Modding;

/// <summary>
/// Base interface for all registry types. Provides cleanup functionality.
/// </summary>
[PublicAPI]
public interface IRegistryBase
{
    /// <summary>
    /// Unregisters an object from the registry by its identification.
    /// </summary>
    /// <param name="id">The identification of the object to unregister.</param>
    void Unregister(Identification id);
}

/// <summary>
/// Base interface for registry declarations. Implementing classes must be partial and annotated with <see cref="RegistryAttribute"/>.
/// Source generators create registration attributes (from [RegistryMethod]) and configurators for types implementing this interface.
/// </summary>
[PublicAPI]
public interface IRegistry : IRegistryBase
{
    /// <summary>
    /// Gets the registry category identifier.
    /// </summary>
    static abstract string Identifier { get; }

    /// <summary>Optional resource sub-folder this registry loads files from; null when it owns no files.</summary>
    static virtual string? ResourceFolder => null;
}

/// <summary>
/// Registry contract carrying a type-encoded link to the module that owns it. Every registry declares
/// its owning module through <typeparamref name="TModule"/>; the manager reads this link to add and remove
/// the registry automatically over the module's lifecycle. The <c>[Registry(Identifier = "...")]</c>
/// attribute remains the source-generation marker — the module link lives on the type argument, not the
/// attribute.
/// </summary>
/// <typeparam name="TModule">The owning module. <see cref="IHasIdentification"/> makes
/// <c>TModule.Identification</c> compile-guaranteed.</typeparam>
[PublicAPI]
public interface IRegistry<TModule> : IRegistry where TModule : IHasIdentification, IStateModule
{
    /// <summary>
    /// The identification of the module that owns this registry. Emitted by the source generator into the
    /// registry's generated partial as <c>TModule.Identification</c>.
    /// </summary>
    static abstract Identification OwningModule { get; }
}

