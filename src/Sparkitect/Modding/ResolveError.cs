using JetBrains.Annotations;
using Sparkitect.Utils.DU;
using Sundew.DiscriminatedUnions;

namespace Sparkitect.Modding;

/// <summary>
/// Errors produced by <see cref="IIdentificationManager"/> Resolve-path methods.
/// Payload mirrors the failed input key (string or numeric).
/// </summary>
[DiscriminatedUnion]
[PublicAPI]
public abstract partial record ResolveError
{
    /// <summary>The mod-id was not registered.</summary>
    public sealed partial record UnknownMod(Variant<string, ushort> Value)      : ResolveError;
    /// <summary>The category-id was not registered.</summary>
    public sealed partial record UnknownCategory(Variant<string, ushort> Value) : ResolveError;
    /// <summary>The object-id was not registered under the resolved mod and category.</summary>
    public sealed partial record UnknownObject(Variant<string, ushort> Value)   : ResolveError;
}
