using System.Text.Json;
using System.Text.RegularExpressions;

namespace Bloodcraft.Tools;

internal static class GenerateMessageTranslations
{
    static readonly Regex _serviceRegex = new(
        @"LocalizationService\.Reply\s*\([^,]*,\s*",
        RegexOptions.Compiled);

    static readonly Regex _ctxRegex = new(
        @"ctx\.Reply\s*\(\s*",
        RegexOptions.Compiled);

    public static void Run(string rootPath)
    {
        string messagesDir = Path.Combine(rootPath, "Resources", "Localization", "Messages");
        string engPath = Path.Combine(messagesDir, "English.json");
        if (!File.Exists(engPath))
        {
            Console.WriteLine($"English.json not found at {engPath}");
            return;
        }

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var englishFile = JsonSerializer.Deserialize<MessageFile>(File.ReadAllText(engPath), options)
                          ?? new MessageFile();
        englishFile.Messages ??= new Dictionary<string, string>();

        HashSet<string> strings = [];

        foreach (string file in Directory.GetFiles(rootPath, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains("/obj/") || file.Contains("/bin/"))
                continue;
            string content = File.ReadAllText(file);
            foreach (Match m in _serviceRegex.Matches(content))
            {
                string? lit = ExtractLiteral(content, m.Index + m.Length);
                if (lit != null)
                    strings.Add(UnescapeLiteral(lit));
            }
            foreach (Match m in _ctxRegex.Matches(content))
            {
                string? lit = ExtractLiteral(content, m.Index + m.Length);
                if (lit != null)
                    strings.Add(UnescapeLiteral(lit));
            }
        }

        foreach (string s in strings)
        {
            uint hash = ComputeHash(s);
            englishFile.Messages[hash.ToString()] = s;
        }

        File.WriteAllText(engPath, JsonSerializer.Serialize(englishFile, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("English.json updated.");
    }

    static string? ExtractLiteral(string content, int startIndex)
    {
        int i = startIndex;
        while (i < content.Length && char.IsWhiteSpace(content[i])) i++;

        int literalStart = i;
        bool verbatim = false;
        bool interpolated = false;

        if (i < content.Length && content[i] == '@')
        {
            verbatim = true;
            i++;
        }
        if (i < content.Length && content[i] == '$')
        {
            interpolated = true;
            i++;
        }
        if (i < content.Length && content[i] == '@')
        {
            verbatim = true;
            i++;
        }

        if (i >= content.Length || content[i] != '"')
            return null;

        i++;
        int braceDepth = 0;

        while (i < content.Length)
        {
            char c = content[i];

            if (!verbatim && c == '\\')
            {
                i += 2;
                continue;
            }

            if (interpolated)
            {
                if (c == '{')
                {
                    braceDepth++;
                    i++;
                    continue;
                }
                if (c == '}' && braceDepth > 0)
                {
                    braceDepth--;
                    i++;
                    continue;
                }
                if (braceDepth > 0)
                {
                    if (c == '$' && i + 1 < content.Length && content[i + 1] == '"')
                    {
                        string? nested = ExtractLiteral(content, i);
                        if (nested != null)
                        {
                            i += nested.Length;
                            continue;
                        }
                    }
                    else if (c == '"')
                    {
                        string? nested = ExtractLiteral(content, i);
                        if (nested != null)
                        {
                            i += nested.Length;
                            continue;
                        }
                    }
                }
            }

            if (c == '"' && braceDepth == 0)
            {
                if (verbatim && i + 1 < content.Length && content[i + 1] == '"')
                {
                    i += 2;
                    continue;
                }
                return content.Substring(literalStart, i - literalStart + 1);
            }

            i++;
        }

        return null;
    }

    static string UnescapeLiteral(string literal)
    {
        bool verbatim = literal.StartsWith("@\"") || literal.StartsWith("$@\"") || literal.StartsWith("@$\"");
        int firstQuote = literal.IndexOf('"');
        string content = literal.Substring(firstQuote + 1, literal.Length - firstQuote - 2);
        return verbatim ? content.Replace("\"\"", "\"") : Regex.Unescape(content);
    }

    static uint ComputeHash(string englishText)
    {
        const uint offset = 2166136261;
        const uint prime = 16777619;
        uint hash = offset;
        foreach (char c in englishText)
        {
            hash ^= c;
            hash *= prime;
        }
        return hash;
    }

    class MessageFile
    {
        public Dictionary<string, string> Messages { get; set; } = new();
    }
}
