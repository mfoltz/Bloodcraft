using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Bloodcraft.Tools;
internal static class CheckTranslations
{
    public static void Run(string rootPath, bool showText = false, string? summaryJsonPath = null)
    {
        englishStopWords = LoadStopWords(rootPath);
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

        var summary = new Dictionary<string, LanguageSummary>(StringComparer.OrdinalIgnoreCase);
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
            string langName = Path.GetFileName(path);
            summary[langName] = new LanguageSummary
            {
                Missing = missing.Count,
                EnglishLike = englishLike.Count
            };

            if (missing.Count > 0)
            {
                Console.WriteLine($"Missing hashes in {Path.GetFileName(path)}:");
                foreach (string h in missing)
                {
                    if (showText)
                    {
                        string eng = englishFile.Messages.GetValueOrDefault(h, string.Empty);
                        Console.WriteLine($"  {h}: {eng}");
                    }
                    else
                    {
                        Console.WriteLine($"  {h}");
                    }
                }
            }

            if (englishLike.Count > 0)
            {
                Console.WriteLine($"Potential untranslated strings in {Path.GetFileName(path)}:");
                foreach (string h in englishLike)
                {
                    if (showText)
                    {
                        string eng = englishFile.Messages.GetValueOrDefault(h, string.Empty);
                        string current = langFile.Messages[h];
                        Console.WriteLine($"  {h}: {eng} -> {current}");
                    }
                    else
                    {
                        Console.WriteLine($"  {h}");
                    }
                }
            }
        }

        int totalMissing = summary.Values.Sum(s => s.Missing);
        int totalEnglishLike = summary.Values.Sum(s => s.EnglishLike);

        if (!string.IsNullOrEmpty(summaryJsonPath))
        {
            var json = new
            {
                Languages = summary,
                Totals = new { Missing = totalMissing, EnglishLike = totalEnglishLike }
            };
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            File.WriteAllText(summaryJsonPath, JsonSerializer.Serialize(json, jsonOptions));
        }

        if (totalMissing > 0 || totalEnglishLike > 0)
        {
            if (!string.IsNullOrEmpty(summaryJsonPath))
                Console.WriteLine($"Issues found. See {summaryJsonPath} for details.");
            Environment.Exit(1);
        }
    }

    class MessageFile
    {
        public Dictionary<string, string> Messages { get; set; } = new();
    }

    class LanguageSummary
    {
        public int Missing { get; set; }
        public int EnglishLike { get; set; }
    }

    static HashSet<string> englishStopWords = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Load the English stop words file from the repository.
    /// </summary>
    /// <param name="rootPath">Root path of the repository.</param>
    /// <returns>Set containing stop words for English detection.</returns>
    static HashSet<string> LoadStopWords(string rootPath)
    {
        string path = Path.Combine(rootPath, "Tools", "english_stopwords.txt");
        if (!File.Exists(path))
            return new(StringComparer.OrdinalIgnoreCase);

        return new HashSet<string>(
            File.ReadAllLines(path).Where(l => !string.IsNullOrWhiteSpace(l)),
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the provided text appears to be English by checking for common stop words.
    /// </summary>
    /// <param name="txt">Text to examine.</param>
    /// <returns><see langword="true"/> if the text contains English stop words; otherwise, <see langword="false"/>.</returns>
    static bool LooksEnglish(string txt)
    {
        if (string.IsNullOrWhiteSpace(txt))
            return false;

        var words = Regex.Matches(txt, "\\b\\w+\\b").Select(m => m.Value);
        return words.Any(w => englishStopWords.Contains(w));
    }
}
