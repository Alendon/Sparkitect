using JetBrains.ReSharper.Psi;

namespace Sparkitect.RiderPlugin.Registrations;

/// <summary>
/// The registration owner edge read from a leaf's <c>[RegisteredFrom]</c> coordinate. Exactly one of the
/// two coordinate shapes is populated: a C# <see cref="Type" /> (optionally with a <see cref="Member" />)
/// for source-driven registrations, or a resource-file <see cref="SourcePath" /> coordinate for YAML.
/// </summary>
public sealed class RegistrationOwner
{
    private RegistrationOwner(
        ITypeElement? type,
        string? member,
        string? sourcePath,
        int sourceLine,
        int sourceColumn)
    {
        Type = type;
        Member = member;
        SourcePath = sourcePath;
        SourceLine = sourceLine;
        SourceColumn = sourceColumn;
    }

    /// <summary>The originating C# type, or null when the owner is a resource-file coordinate.</summary>
    public ITypeElement? Type { get; }

    /// <summary>The originating member on <see cref="Type" />, when present.</summary>
    public string? Member { get; }

    /// <summary>The resource-file path of the registration site, or null when the owner is a C# type.</summary>
    public string? SourcePath { get; }

    /// <summary>The line within <see cref="SourcePath" />.</summary>
    public int SourceLine { get; }

    /// <summary>The column within <see cref="SourcePath" />.</summary>
    public int SourceColumn { get; }

    /// <summary>A source-driven owner: the originating type with an optional member.</summary>
    public static RegistrationOwner ForType(ITypeElement type, string? member) =>
        new(type, member, null, 0, 0);

    /// <summary>A resource-file owner: a plain path coordinate.</summary>
    public static RegistrationOwner ForResource(string sourcePath, int sourceLine, int sourceColumn) =>
        new(null, null, sourcePath, sourceLine, sourceColumn);
}
