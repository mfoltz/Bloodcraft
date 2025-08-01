using System.Text.Json;
using System.Text.RegularExpressions;

namespace Bloodcraft.Tools;
internal static class CheckTranslations
{
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
        var englishFile = JsonSerializer.Deserialize<MessageFile>(File.ReadAllText(engPath), options) ?? new MessageFile();
        englishFile.Messages ??= new Dictionary<string, string>();

        foreach (string path in Directory.GetFiles(messagesDir, "*.json"))
        {
            if (Path.GetFileName(path).Equals("English.json", StringComparison.OrdinalIgnoreCase))
                continue;

            var langFile = JsonSerializer.Deserialize<MessageFile>(File.ReadAllText(path), options) ?? new MessageFile();
            langFile.Messages ??= new Dictionary<string, string>();

            List<string> missing = [];
            List<string> englishLike = [];
            foreach (string hash in englishFile.Messages.Keys)
            {
                if (!langFile.Messages.ContainsKey(hash))
                    missing.Add(hash);
                else if (LooksEnglish(langFile.Messages[hash]))
                    englishLike.Add(hash);
            }

            if (missing.Count > 0)
            {
                Console.WriteLine($"Missing hashes in {Path.GetFileName(path)}:");
                foreach (string h in missing)
                    Console.WriteLine($"  {h}");
            }

            if (englishLike.Count > 0)
            {
                Console.WriteLine($"Potential untranslated strings in {Path.GetFileName(path)}:");
                foreach (string h in englishLike)
                    Console.WriteLine($"  {h}");
            }
        }
    }

    class MessageFile
    {
        public Dictionary<string, string> Messages { get; set; } = new();
    }

    static readonly Regex EnglishWords = new(
        @"\b(the|and|of|with|you|your|for|an)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    static bool LooksEnglish(string txt)
    {
        if (string.IsNullOrWhiteSpace(txt))
            return false;

        return EnglishWords.IsMatch(txt);
    }
}
