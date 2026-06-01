using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using Sparkitect.RiderPlugin.References;

namespace Sparkitect.RiderPlugin.Registrations;

/// <summary>
/// A type registration: the registered concrete type IS the identity, recognised as owning its
/// generated <c>IHasIdentification</c> leaf. The bonded payload is that owning type itself, taken from
/// the leaf's <c>[RegisteredFrom]</c> coordinate.
/// </summary>
public sealed class TypeRegistration : Registration
{
    public TypeRegistration(ITreeNode anchor, RegistrationKey key)
        : base(anchor, key)
    {
    }

    /// <summary>The owning type carries the identity; the bonded payload is the same type.</summary>
    public override RegistrationOwner? ResolveOwner(IProperty leaf) => RegisteredFromReader.Read(leaf);
}
