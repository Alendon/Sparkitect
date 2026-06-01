using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using Sparkitect.RiderPlugin.References;

namespace Sparkitect.RiderPlugin.Registrations;

/// <summary>
/// An external registration whose anchor is always present but whose payload is optional on both axes:
/// it exposes an optional File reference AND an optional Type(Member) reference, each surfaced only when
/// the leaf's <c>[RegisteredFrom]</c> coordinate carries it. Stateless and ECS-system functions live here
/// with the Type(Member) reference populated (via <c>[RegisteredFrom(typeof(owner), Member=…)]</c>) — the
/// owner is read from the attribute, never the registered <c>*Func</c> wrapper type. A truly-external
/// registration may carry neither payload; the missing payload never weakens the anchor or moves the Go-to
/// target.
/// </summary>
public sealed class ExternalRegistration : Registration
{
    public ExternalRegistration(ITreeNode anchor, RegistrationKey key)
        : base(anchor, key)
    {
    }

    /// <summary>The optional Type(Member) payload, present when <c>[RegisteredFrom]</c> carries a type.</summary>
    public ExternalTypePayload? TypePayload(IProperty leaf)
    {
        var owner = RegisteredFromReader.Read(leaf);
        return owner?.Type == null ? null : new ExternalTypePayload(owner.Type, owner.Member);
    }

    /// <summary>The optional File payload, present when <c>[RegisteredFrom]</c> carries a resource coordinate.</summary>
    public ExternalFilePayload? FilePayload(IProperty leaf)
    {
        var owner = RegisteredFromReader.Read(leaf);
        return string.IsNullOrEmpty(owner?.SourcePath)
            ? null
            : new ExternalFilePayload(owner!.SourcePath!, owner.SourceLine, owner.SourceColumn);
    }

    /// <summary>The shared owner edge; either payload axis may be absent.</summary>
    public override RegistrationOwner? ResolveOwner(IProperty leaf) => RegisteredFromReader.Read(leaf);
}

/// <summary>The optional Type(Member) payload of an <see cref="ExternalRegistration" />.</summary>
public sealed class ExternalTypePayload
{
    public ExternalTypePayload(ITypeElement type, string? member)
    {
        Type = type;
        Member = member;
    }

    public ITypeElement Type { get; }
    public string? Member { get; }
}

/// <summary>The optional File payload of an <see cref="ExternalRegistration" />.</summary>
public sealed class ExternalFilePayload
{
    public ExternalFilePayload(string sourcePath, int sourceLine, int sourceColumn)
    {
        SourcePath = sourcePath;
        SourceLine = sourceLine;
        SourceColumn = sourceColumn;
    }

    public string SourcePath { get; }
    public int SourceLine { get; }
    public int SourceColumn { get; }
}
