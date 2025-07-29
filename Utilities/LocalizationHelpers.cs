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
    public static string ProtectTokens(string message, IDictionary<string, string> map)
    {
        int tagIndex = 0;
        int varIndex = 0;
        int csIndex = 0;

        message = RichTextTag.Replace(message, m =>
        {
            string key = $"TAG_{tagIndex++}";
            map[key] = m.Value;
            return $"[[{key}]]";
        });

        message = Placeholder.Replace(message, m =>
        {
            string key = $"VAR_{varIndex++}";
            map[key] = m.Value;
            return $"[[{key}]]";
        });

        message = CsInterp.Replace(message, m =>
        {
            string key = $"CS_{csIndex++}";
            map[key] = m.Value;
            return $"[[{key}]]";
        });

        return message;
    }

    /// <summary>
    /// Restores tokens from the map back into the message string.
    /// </summary>
    public static string UnprotectTokens(string message, IDictionary<string, string> map)
    {
        return Regex.Replace(message, @"\[\[(TAG|VAR|CS)_(\d+)\]\]", m =>
        {
            string key = $"{m.Groups[1].Value}_{m.Groups[2].Value}";
            return map.TryGetValue(key, out string value) ? value : m.Value;
        });
    }
}
