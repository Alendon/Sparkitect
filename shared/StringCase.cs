using System;

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

    /// <summary>
    /// Checks if a string is in strict snake_case format as required by Sparkitect naming validation.
    /// Rules:
    /// - Only lowercase letters (a-z), digits (0-9), and underscores
    /// - Must start with a letter
    /// - No consecutive underscores
    /// - No leading or trailing underscores
    /// - No dots
    /// </summary>
    internal static bool IsStrictSnakeCase(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;

        // Must start with a letter
        if (s[0] < 'a' || s[0] > 'z') return false;

        // No trailing underscore
        if (s[s.Length - 1] == '_') return false;

        bool prevWasUnderscore = false;
        for (int i = 0; i < s.Length; i++)
        {
            var ch = s[i];

            if (ch == '_')
            {
                // No consecutive underscores
                if (prevWasUnderscore) return false;
                prevWasUnderscore = true;
                continue;
            }

            prevWasUnderscore = false;

            // Only a-z and 0-9 allowed (dots explicitly disallowed)
            if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9')) continue;

            return false;
        }

        return true;
    }
}
