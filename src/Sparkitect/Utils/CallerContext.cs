namespace Sparkitect.Utils;

/// <summary>
/// Holds caller location information captured at compile time.
/// </summary>
public readonly record struct CallerContext(string FilePath, int LineNumber)
{
    /// <summary>
    /// Gets the file name without directory path.
    /// </summary>
    public string FileName => System.IO.Path.GetFileName(FilePath);

    /// <summary>
    /// Returns a formatted string for logging: "FileName:LineNumber"
    /// </summary>
    public override string ToString() => $"{FileName}:{LineNumber}";
}
