using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using Sparkitect.RiderPlugin.References;

namespace Sparkitect.RiderPlugin.Registrations;

/// <summary>
/// A resource-file registration: the payload is the resource-file coordinate (<c>SourcePath</c> plus
/// line/column → the YAML entry key), read from the leaf's <c>[RegisteredFrom]</c> coordinate. The anchor
/// is the YAML entry key itself.
/// </summary>
public sealed class ResourceRegistration : Registration
{
    public ResourceRegistration(ITreeNode anchor, RegistrationKey key)
        : base(anchor, key)
    {
    }

    /// <summary>The file payload is the <c>[RegisteredFrom]</c> resource coordinate.</summary>
    public override RegistrationOwner? ResolveOwner(IProperty leaf) => RegisteredFromReader.Read(leaf);
}
