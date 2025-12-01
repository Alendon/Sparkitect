namespace Sparkitect.Modding;

/// <summary>
/// Base interface for all registry types. Provides cleanup functionality.
/// </summary>
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
public interface IRegistry : IRegistryBase
{
    /// <summary>
    /// Gets the registry category identifier.
    /// </summary>
    static abstract string Identifier { get; }

    static virtual string? ResourceFolder => null;
}

