namespace Sparkitect.Utilities;

/// <summary>
/// Provides string case conversion utilities.
/// </summary>
internal static class StringCase
{
    /// <summary>
    /// Converts a string to snake_case.
    /// </summary>
    public static string ToSnakeCase(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return s;
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (char.IsUpper(c))
            {
                if (i > 0) sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else sb.Append(c);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Converts a string to PascalCase.
    /// </summary>
    public static string ToPascalCase(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return s;
        var parts = s.Split(new[] { '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var sb = new System.Text.StringBuilder();
        foreach (var p in parts)
        {
            if (p.Length == 0) continue;
            sb.Append(char.ToUpperInvariant(p[0]));
            if (p.Length > 1) sb.Append(p.Substring(1));
        }
        return sb.ToString();
    }

    /// <summary>
    /// Checks if a string is in snake_case format.
    /// </summary>
    public static bool IsSnakeCase(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        for (int i = 0; i < s.Length; i++)
        {
            var ch = s[i];
            if (ch == '_') continue;
            if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9')) continue;
            return false;
        }
        return true;
    }
}
