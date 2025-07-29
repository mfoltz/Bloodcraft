using System.Text;
using System.Text.RegularExpressions;

namespace Bloodcraft.Utilities;

/// <summary>
/// Utility methods to protect Unity rich-text tags and placeholders during translation.
/// </summary>
internal static class LocalizationHelpers
{
    static readonly Regex RichTextTag = new(
        @"</?\w+[^>]*>",
        RegexOptions.Compiled);

    static readonly Regex Placeholder = new(
        @"\{[^}]+\}",
        RegexOptions.Compiled);

    static readonly Regex CsInterp = new(
        @"\$\{[^}]+\}",
        RegexOptions.Compiled);

    /// <summary>
    /// Replaces tags and placeholders with indexed markers so translation services do not alter them.
    /// </summary>
    public static (string safe, List<string> tokens) Protect(string message)
    {
        List<string> tokens = [];
        string result = message;

        foreach (Regex regex in new[] { RichTextTag, Placeholder, CsInterp })
        {
            result = regex.Replace(result, m =>
            {
                tokens.Add(m.Value);
                return $"[[TOKEN_{tokens.Count - 1}]]";
            });
        }

        return (result, tokens);
    }

    /// <summary>
    /// Restores tokens back into the message string using the provided list.
    /// </summary>
    public static string Unprotect(string message, IList<string> tokens)
    {
        return Regex.Replace(message, @"\[\[TOKEN_(\d+)\]\]", m =>
        {
            if (int.TryParse(m.Groups[1].Value, out int index) && index >= 0 && index < tokens.Count)
            {
                return tokens[index];
            }
            return m.Value;
        });
    }

    /// <summary>
    /// Backwards compatible wrapper for the old API.
    /// </summary>
    public static string ProtectTokens(string message, IDictionary<string, string> map)
    {
        var (safe, tokens) = Protect(message);
        for (int i = 0; i < tokens.Count; i++)
            map[$"TOKEN_{i}"] = tokens[i];
        return safe;
    }

    /// <summary>
    /// Backwards compatible wrapper for the old API.
    /// </summary>
    public static string UnprotectTokens(string message, IDictionary<string, string> map)
    {
        List<string> list = new();
        int index = 0;
        while (map.TryGetValue($"TOKEN_{index}", out string? value))
        {
            list.Add(value);
            index++;
        }
        return Unprotect(message, list);
    }
}