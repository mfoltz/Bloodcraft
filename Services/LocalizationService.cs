using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using System.Reflection;
using System.Text.Json;
using Unity.Entities;
using VampireCommandFramework;

namespace Bloodcraft.Services;
internal class LocalizationService
{
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

    static readonly string _language = ConfigService.LanguageLocalization;
    static readonly Dictionary<string, string> _localizedLanguages = new()
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

    static readonly Dictionary<int, string> _prefabHashesToGuidStrings = [];
    static readonly Dictionary<string, string> _guidStringsToLocalizedNames = [];
    public LocalizationService()
    {
        InitializeLocalizations();
    }
    static void InitializeLocalizations()
    {
        LoadPrefabHashesToGuidStrings();
        LoadGuidStringsToLocalizedNames();
    }
    static void LoadPrefabHashesToGuidStrings()
    {
        string resourceName = "Bloodcraft.Localization.Prefabs.json";

        Assembly assembly = Assembly.GetExecutingAssembly();
        Stream stream = assembly.GetManifestResourceStream(resourceName);

        using StreamReader reader = new(stream);
        string jsonContent = reader.ReadToEnd();

        var prefabNames = JsonSerializer.Deserialize<Dictionary<int, string>>(jsonContent);
        prefabNames
            .ForEach(kvp => _prefabHashesToGuidStrings[kvp.Key] = kvp.Value);
    }
    static void LoadGuidStringsToLocalizedNames()
    {
        string resourceName = _localizedLanguages.ContainsKey(_language) ? _localizedLanguages[_language] : "Bloodcraft.Localization.English.json";

        Assembly assembly = Assembly.GetExecutingAssembly();
        Stream stream = assembly.GetManifestResourceStream(resourceName);

        using StreamReader localizationReader = new(stream);
        string jsonContent = localizationReader.ReadToEnd();

        var localizationFile = JsonSerializer.Deserialize<LocalizationFile>(jsonContent);
        localizationFile.Nodes
            .ToDictionary(x => x.Guid, x => x.Text)
            .ForEach(kvp => _guidStringsToLocalizedNames[kvp.Key] = kvp.Value);
    }
    internal static void HandleReply(ChatCommandContext ctx, string message)
    {
        ctx.Reply(message);
    }
    internal static void HandleServerReply(EntityManager entityManager, User user, string message)
    {
        ServerChatUtils.SendSystemMessageToClient(entityManager, user, message);
    }
    public static string GetAssetGuidString(PrefabGUID prefabGUID)
    {
        if (_prefabHashesToGuidStrings.TryGetValue(prefabGUID.GuidHash, out var guidString))
        {
            return guidString;
        }

        return string.Empty;
    }
    public static string GetGuidString(PrefabGUID prefabGUID)
    {
        if (_prefabHashesToGuidStrings.TryGetValue(prefabGUID.GuidHash, out string guidString))
        {
            return guidString;
        }

        return string.Empty;
    }
    public static string GetNameFromGuidString(string guidString)
    {
        if (_guidStringsToLocalizedNames.TryGetValue(guidString, out string localizedName))
        {
            return localizedName;
        }

        return string.Empty;
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
