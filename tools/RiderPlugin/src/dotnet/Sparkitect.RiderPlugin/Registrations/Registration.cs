using JetBrains.DocumentModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace Sparkitect.RiderPlugin.Registrations;

/// <summary>
/// A registration site recognised through the structured metadata contract: the authoritative
/// identifier-string anchor (the C# id literal or the YAML entry key) plus the resolved
/// <see cref="RegistrationKey" /> that bonds it to the generated leaf id property.
/// </summary>
/// <remarks>
/// The axis of variation is Registration (uniform — every subtype shares the same anchor and the same
/// navigable Go-to target) versus Payload (kind-specific — each subtype owns only how its payload is
/// resolved). Owner-resolution always reads the leaf's <c>[RegisteredFrom]</c> coordinate; no subtype
/// reconstructs ownership heuristically and none follows the registered <c>*Func</c> wrapper type.
/// </remarks>
public abstract class Registration
{
    protected Registration(ITreeNode anchor, Sparkitect.RiderPlugin.References.RegistrationKey key)
    {
        Anchor = anchor;
        Key = key;
    }

    /// <summary>The authoritative declaration: the id string literal or YAML entry-key node.</summary>
    public ITreeNode Anchor { get; }

    /// <summary>The pivot bonding this anchor to the generated <c>{Mod}{Category}IDs.{Member}</c> leaf.</summary>
    public Sparkitect.RiderPlugin.References.RegistrationKey Key { get; }

    /// <summary>
    /// The navigable Go-to-Registration target — always the anchor's document range (the literal or
    /// YAML key), never the payload. A missing or absent payload never changes this target.
    /// </summary>
    public DocumentRange NavigableTarget => Anchor.GetDocumentRange();

    /// <summary>
    /// Resolves the registration owner from the generated leaf's <c>[RegisteredFrom]</c> coordinate.
    /// Subtypes layer their payload-specific surface on top of this shared owner edge.
    /// </summary>
    /// <param name="leaf">The generated leaf id property the anchor resolves to.</param>
    public abstract RegistrationOwner? ResolveOwner(IProperty leaf);
}
