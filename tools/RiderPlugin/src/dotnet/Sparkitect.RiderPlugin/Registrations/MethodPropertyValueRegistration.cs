using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using Sparkitect.RiderPlugin.References;

namespace Sparkitect.RiderPlugin.Registrations;

/// <summary>
/// A method/property-value registration: the payload is the provider member (the method or property that
/// supplies the registered value), reached through the leaf's <c>[RegisteredFrom(typeof(owner), Member=…)]</c>
/// coordinate. The member name comes from the attribute, never from the registered value's runtime type.
/// </summary>
public sealed class MethodPropertyValueRegistration : Registration
{
    public MethodPropertyValueRegistration(ITreeNode anchor, RegistrationKey key)
        : base(anchor, key)
    {
    }

    /// <summary>The provider member payload is the <c>[RegisteredFrom]</c> type + <c>Member</c>.</summary>
    public override RegistrationOwner? ResolveOwner(IProperty leaf) => RegisteredFromReader.Read(leaf);
}
