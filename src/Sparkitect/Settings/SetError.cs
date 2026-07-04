using JetBrains.Annotations;
using Sparkitect.Modding;
using Sundew.DiscriminatedUnions;

namespace Sparkitect.Settings;

/// <summary>
/// Errors produced by the settings write primitive. Payload names the offending source or setting id.
/// </summary>
[DiscriminatedUnion]
[PublicAPI]
public abstract partial record SetError
{
    /// <summary>The target source exists but refuses writes (e.g. the CLI or engine-config source).</summary>
    public sealed partial record SourceReadonly(Identification SourceId) : SetError;

    /// <summary>No source is registered under the target source id.</summary>
    public sealed partial record UnknownSource(Identification SourceId) : SetError;

    /// <summary>No setting is declared under the target setting id.</summary>
    public sealed partial record UnknownSetting(Identification Id) : SetError;
}
