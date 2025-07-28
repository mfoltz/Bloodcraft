using System.Text;
using System.Text.RegularExpressions;

namespace Bloodcraft.Utilities;

/// <summary>
/// Utility methods to protect Unity rich-text tags and placeholders during translation.
/// </summary>
internal static class LocalizationHelpers
{
    static readonly Regex _tagPattern = new(
        @"</?color=[^>]+>|</?b>|</?size=[^>]+>",
        RegexOptions.Compiled);

    static readonly Regex _varPattern = new(
        @"\{[^}]+\}",
        RegexOptions.Compiled);

    /// <summary>
    /// Replaces tags and variable placeholders with base64 markers
    /// so translation services do not alter them.
    /// </summary>
    public static string ProtectTokens(string message)
    {
        message = _tagPattern.Replace(message, m => $"[[TAG_{Convert.ToBase64String(Encoding.UTF8.GetBytes(m.Value))}]]");
        message = _varPattern.Replace(message, m => $"[[VAR_{Convert.ToBase64String(Encoding.UTF8.GetBytes(m.Value))}]]");
        return message;
    }

    /// <summary>
    /// Restores tags and variables from base64 markers.
    /// </summary>
    public static string UnprotectTokens(string message)
    {
        return Regex.Replace(message, @"\[\[TAG_(.*?)\]\]|\[\[VAR_(.*?)\]\]", m =>
        {
            string data = m.Groups[1].Value.Length > 0 ? m.Groups[1].Value : m.Groups[2].Value;
            return Encoding.UTF8.GetString(Convert.FromBase64String(data));
        });
    }
}
