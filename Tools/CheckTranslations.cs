using System.Text.Json;

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
            foreach (string hash in englishFile.Messages.Keys)
            {
                if (!langFile.Messages.ContainsKey(hash))
                    missing.Add(hash);
            }

            if (missing.Count > 0)
            {
                Console.WriteLine($"Missing hashes in {Path.GetFileName(path)}:");
                foreach (string h in missing)
                    Console.WriteLine($"  {h}");
            }
        }
    }

    class MessageFile
    {
        public Dictionary<string, string> Messages { get; set; } = new();
    }
}
