using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.GameState;

/// <summary>
/// Defines a state descriptor - a specific runtime configuration composed from modules.
/// States form a parent-child hierarchy and define which modules they include.
/// </summary>
[TypedRegistrationContract]
[PublicAPI]
public interface IStateDescriptor
{
    /// <summary>
    /// Gets the identification of the parent state. States can only transition to immediate parent or children.
    /// </summary>
    static abstract Identification ParentId { get; }
    

    /// <summary>
    /// Gets the modules this state introduces (delta from parent). Inherited modules are automatic.
    /// </summary>
    static abstract IReadOnlyList<Identification> Modules { get; }
}