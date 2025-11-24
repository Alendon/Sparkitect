using System.Collections.Generic;
using System.ComponentModel;
using Sparkitect.DI.Container;
using Sparkitect.Modding;

namespace Sparkitect.GameState;

/// <summary>
/// Defines a state descriptor - a specific runtime configuration composed from modules.
/// States form a parent-child hierarchy and define which modules they include.
/// </summary>
public interface IStateDescriptor
{
    /// <summary>
    /// Gets the identification of the parent state. States can only transition to immediate parent or children.
    /// </summary>
    static abstract Identification ParentId { get; }

    /// <summary>
    /// Gets the unique identification for this state.
    /// </summary>
    static abstract Identification Identification { get; }

    /// <summary>
    /// Gets the modules this state introduces (delta from parent). Inherited modules are automatic.
    /// </summary>
    static abstract IReadOnlyList<Identification> Modules { get; }
}

/// <summary>
/// Internal interface for source-generated state descriptor implementations that contain state functions.
/// </summary>
public interface IStateDescriptorMethods
{
    /// <summary>
    /// Gets the state functions defined in this state descriptor.
    /// </summary>
    public IReadOnlyList<IStateMethod> ContainingMethods { get; }
}

/// <summary>
/// Internal interface for source-generated state function wrappers.
/// </summary>
public interface IStateMethod
{
    /// <summary>
    /// Executes the state function.
    /// </summary>
    public void Execute();

    /// <summary>
    /// Initializes the state function wrapper with DI container and facade mappings.
    /// </summary>
    /// <param name="container">The DI container for resolving dependencies.</param>
    /// <param name="facadeMap">Type substitution map for facade resolution.</param>
    public void Initialize(ICoreContainer container, IReadOnlyDictionary<Type, Type> facadeMap);
}

