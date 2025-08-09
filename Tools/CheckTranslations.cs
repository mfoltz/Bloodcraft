using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Bloodcraft.Tools;
internal static class CheckTranslations
{
    public static void Run(string rootPath, bool showText = false)
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
    }

    class MessageFile
    {
        public Dictionary<string, string> Messages { get; set; } = new();
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
