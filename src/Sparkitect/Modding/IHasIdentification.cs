namespace Sparkitect.Modding;

public interface IHasIdentification
{
    public static abstract Identification Identification { get; }
}

/// <summary>
/// Helper to read Identification from types implementing IHasIdentification.
/// Used by generated code to pass identification via type rather than expression.
/// </summary>
public static class IdentificationHelper
{
    public static Identification Read<T>() where T : IHasIdentification => T.Identification;
}