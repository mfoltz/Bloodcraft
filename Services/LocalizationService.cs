using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Stunlock.Localization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Unity.Entities;
using VampireCommandFramework;
using Match = System.Text.RegularExpressions.Match;
using Regex = System.Text.RegularExpressions.Regex;
using RegexOptions = System.Text.RegularExpressions.RegexOptions;

namespace Bloodcraft.Services;
internal class LocalizationService
{
    static readonly Regex Regex = new(@"(?<open>\<.*?\>)|(?<word>\b\w+(?:'\w+)?\b)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    static readonly string Language = ConfigService.LanguageLocalization;
    struct Code
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
    }
    struct Node
    {
        public string Guid { get; set; }
        public string Text { get; set; }
    }
    struct Words
    {
        public string Original { get; set; }
        public string Translation { get; set; }
    }
    struct LocalizationFile
    {
        public Code[] Codes { get; set; }
        public Node[] Nodes { get; set; }
        public Words[] Words { get; set; }
    }

    static readonly Dictionary<string, string> Localization = [];
    static readonly Dictionary<int, string> PrefabNames = [];
    static readonly Dictionary<string, string> LocalizedWords = [];

    static readonly Dictionary<string, string> LocalizationMapping = new()
    {
        {"English", "Bloodcraft.Localization.English.json"},
        {"German", "Bloodcraft.Localization.German.json"},
        {"French", "Bloodcraft.Localization.French.json"},
        {"Spanish", "Bloodcraft.Localization.Spanish.json"},
        {"Italian", "Bloodcraft.Localization.Italian.json"},
        {"Japanese", "Bloodcraft.Localization.Japanese.json"},
        {"Koreana", "Bloodcraft.Localization.Koreana.json"},
        {"Portuguese", "Bloodcraft.Localization.Portuguese.json"},
        {"Russian", "Bloodcraft.Localization.Russian.json"},
        {"SimplifiedChinese", "Bloodcraft.Localization.SChinese.json"},
        {"TraditionalChinese", "Bloodcraft.Localization.TChinese.json"},
        {"Hungarian", "Bloodcraft.Localization.Hungarian.json"},
        {"Latam", "Bloodcraft.Localization.Latam.json"},
        {"Polish", "Bloodcraft.Localization.Polish.json"},
        {"Thai", "Bloodcraft.Localization.Thai.json"},
        {"Turkish", "Bloodcraft.Localization.Turkish.json"},
        {"Vietnamese", "Bloodcraft.Localization.Vietnamese.json"},
        {"Brazilian", "Bloodcraft.Localization.Brazilian.json"}
    };
    public LocalizationService()
    {
        LoadLocalizations();
        LoadPrefabNames();
    }
    static void LoadLocalizations()
    {
        var resourceName = LocalizationMapping.ContainsKey(Language) ? LocalizationMapping[Language] : "Bloodcraft.Localization.English.json";

        var assembly = Assembly.GetExecutingAssembly();
        var stream = assembly.GetManifestResourceStream(resourceName);

        using StreamReader localizationReader = new(stream);
        string jsonContent = localizationReader.ReadToEnd();
        var localizationFile = JsonSerializer.Deserialize<LocalizationFile>(jsonContent);

        localizationFile.Nodes
            .ToDictionary(x => x.Guid, x => x.Text)
            .ForEach(kvp => Localization[kvp.Key] = kvp.Value);
    }
    static void LoadPrefabNames()
    {
        var resourceName = "Bloodcraft.Localization.Prefabs.json";
        var assembly = Assembly.GetExecutingAssembly();
        var stream = assembly.GetManifestResourceStream(resourceName);

        using StreamReader reader = new(stream);
        string jsonContent = reader.ReadToEnd();
        var prefabNames = JsonSerializer.Deserialize<Dictionary<int, string>>(jsonContent);
        prefabNames.ForEach(kvp => PrefabNames[kvp.Key] = kvp.Value);
    }
    internal static void HandleReply(ChatCommandContext ctx, string message)
    {
        ctx.Reply(message);

        /*
        if (Language == "English")
        {
            ctx.Reply(message);
        }
        else
        {
            //ctx.Reply(GetLocalizedWords(message));
            ctx.Reply(message);
        }
        */
    }
    internal static void HandleServerReply(EntityManager entityManager, User user, string message)
    {
        ServerChatUtils.SendSystemMessageToClient(entityManager, user, message);

        /*
        if (Language == "English")
        {
            ServerChatUtils.SendSystemMessageToClient(entityManager, user, message);
        }
        else
        {
            ServerChatUtils.SendSystemMessageToClient(entityManager, user, GetLocalizedWords(message));
        }
        */
    }
    static string GetLocalizationFromKey(LocalizationKey key)
    {
        var guid = key.Key.ToGuid().ToString();
        return GetLocalization(guid);
    }
    public static string GetPrefabName(PrefabGUID prefabGUID)
    {
        if (PrefabNames.TryGetValue(prefabGUID.GuidHash, out var itemLocalizationHash))
        {
            return GetLocalization(itemLocalizationHash);
        }
        return prefabGUID.LookupName();
    }
    public static string GetLocalization(string Guid)
    {
        if (Localization.TryGetValue(Guid, out var Text))
        {
            return Text;
        }

        return "Couldn't find key for localization...";
    }

    /*
    static string GetLocalizedWords(string message)
    {
        StringBuilder result = new();
        int lastIndex = 0;

        foreach (Match match in Regex.Matches(message).Cast<Match>())
        {
            result.Append(message, lastIndex, match.Index - lastIndex);

            if (match.Groups["open"].Success)
            {
                // Return tags as is
                result.Append(match.Value);
            }
            else if (match.Groups["word"].Success)
            {
                // Perform case-insensitive replacement for matched words
                string word = match.Value;
                string wordLower = word.ToLower();

                if (LocalizedWords.TryGetValue(wordLower, out string localizedWord))
                {
                    // Preserve the original case of the word
                    if (char.IsUpper(word[0]))
                    {
                        if (localizedWord.Length > 1)
                        {
                            localizedWord = char.ToUpper(localizedWord[0]) + localizedWord[1..];
                        }
                        else
                        {
                            localizedWord = char.ToUpper(localizedWord[0]).ToString();
                        }
                    }
                    result.Append(localizedWord);
                }
                else
                {
                    result.Append(word); // Use the original case of the word
                }
            }
            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < message.Length)
        {
            result.Append(message, lastIndex, message.Length - lastIndex);
        }

        string translatedMessage = result.ToString();
        return translatedMessage;
    }
    */
}
