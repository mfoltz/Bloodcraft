using System.Text.Json;
using System.Text.RegularExpressions;

namespace Bloodcraft.Tools;

internal static class GenerateMessageTranslations
{
    static readonly Regex _serviceRegex = new(
        "LocalizationService\\.Reply\\s*\\([^,]*,\\s*(?<lit>@?\\$?\"(?:[^\"\\]|\\.)*\")",
        RegexOptions.Compiled | RegexOptions.Singleline);

    static readonly Regex _ctxRegex = new(
        "ctx\\.Reply\\s*\\(\\s*(?<lit>@?\\$?\"(?:[^\"\\]|\\.)*\")",
        RegexOptions.Compiled | RegexOptions.Singleline);

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
                strings.Add(UnescapeLiteral(m.Groups["lit"].Value));
            foreach (Match m in _ctxRegex.Matches(content))
                strings.Add(UnescapeLiteral(m.Groups["lit"].Value));
        }

        foreach (string s in strings)
        {
            uint hash = ComputeHash(s);
            englishFile.Messages[hash.ToString()] = s;
        }

        File.WriteAllText(engPath, JsonSerializer.Serialize(englishFile, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("English.json updated.");
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
