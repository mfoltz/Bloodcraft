using ProjectM;
using ProjectM.Network;
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
    static readonly string regexPattern = @"(?<open>\<.*?\>)|(?<word>\b\w+(?:'\w+)?\b)";
    static readonly Regex regex = new(regexPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static readonly string LanguageLocalization = Plugin.LanguageLocalization.Value;
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

    public static Dictionary<string, string> localization = [];
    public static Dictionary<int, string> prefabNames = [];
    public static Dictionary<string, string> localizedWords = [];

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

    static readonly Dictionary<string, string> LanguageMapping = new()
    {
        {"German", "Bloodcraft.Localization.GermanStrings.json"},
        {"French", "Bloodcraft.Localization.FrenchStrings.json"},
        {"Spanish", "Bloodcraft.Localization.SpanishStrings.json"},
        {"Italian", "Bloodcraft.Localization.ItalianStrings.json"},
        //{"Japanese", "Bloodcraft.Localization.JapaneseStrings.json"},
        //{"Koreana", "Bloodcraft.Localization.KoreanaStrings.json"},
        {"Portuguese", "Bloodcraft.Localization.PortugueseStrings.json"},
        {"Russian", "Bloodcraft.Localization.RussianStrings.json"},
        //{"SimplifiedChinese", "Bloodcraft.Localization.SChineseStrings.json"},
        //{"TraditionalChinese", "Bloodcraft.Localization.TChineseStrings.json"},
        {"Hungarian", "Bloodcraft.Localization.HungarianStrings.json"},
        {"Latam", "Bloodcraft.Localization.LatamStrings.json"},
        {"Polish", "Bloodcraft.Localization.PolishStrings.json"},
        //{"Thai", "Bloodcraft.Localization.ThaiStrings.json"},
        {"Turkish", "Bloodcraft.Localization.TurkishStrings.json"},
        //{"Vietnamese", "Bloodcraft.Localization.VietnameseStrings.json"},
        {"Brazilian", "Bloodcraft.Localization.BrazilianStrings.json"}
    };
    public LocalizationService()
    {
        LoadLocalizations();
        LoadPrefabNames();
    }
    static void LoadLocalizations()
    {
        var resourceName = LocalizationMapping.ContainsKey(LanguageLocalization) ? LocalizationMapping[LanguageLocalization] : "Bloodcraft.Localization.English.json";

        //Core.Log.LogInfo($"Loading localization file: {resourceName}");

        var assembly = Assembly.GetExecutingAssembly();
        var stream = assembly.GetManifestResourceStream(resourceName);
        
        using StreamReader localizationReader = new(stream);
        string jsonContent = localizationReader.ReadToEnd();
        var localizationFile = JsonSerializer.Deserialize<LocalizationFile>(jsonContent);
        localization = localizationFile.Nodes.ToDictionary(x => x.Guid, x => x.Text);

        if (LanguageLocalization == "English")
        {
            return;
        }

        resourceName = LanguageMapping.ContainsKey(LanguageLocalization) ? LanguageMapping[LanguageLocalization] : "";

        if (string.IsNullOrEmpty(resourceName)) return;

        stream = assembly.GetManifestResourceStream(resourceName);

        using StreamReader languageReader = new(stream);
        jsonContent = languageReader.ReadToEnd();
        localizationFile = JsonSerializer.Deserialize<LocalizationFile>(jsonContent);
        localizedWords = localizationFile.Words.OrderByDescending(x => x.Original.Length).ToDictionary(x => x.Original.ToLower(), x => x.Translation);
    }

    static void LoadPrefabNames()
    {
        var resourceName = "Bloodcraft.Localization.Prefabs.json";
        var assembly = Assembly.GetExecutingAssembly();
        var stream = assembly.GetManifestResourceStream(resourceName);

        using StreamReader reader = new(stream);
        string jsonContent = reader.ReadToEnd();
        prefabNames = JsonSerializer.Deserialize<Dictionary<int, string>>(jsonContent);
    }
    public static void HandleReply(ChatCommandContext ctx, string message)
    {
        if (LanguageLocalization == "English")
        {
            ctx.Reply(message);
        }
        else
        {
            ctx.Reply(GetLocalizedWords(message));
        }
    }
    public static void HandleServerReply(EntityManager entityManager, User user, string message)
    {
        if (LanguageLocalization == "English")
        {
            ServerChatUtils.SendSystemMessageToClient(entityManager, user, message);
        }
        else
        {
            ServerChatUtils.SendSystemMessageToClient(entityManager, user, GetLocalizedWords(message));
        }
    }
    public static string GetLocalization(string Guid)
    {
        if (localization.TryGetValue(Guid, out var Text))
        {
            return Text;
        }
        return $"<Localization not found for {Guid}>";
    }
    public static string GetLocalizedWords(string message)
    {
        StringBuilder result = new();
        int lastIndex = 0;

        foreach (Match match in regex.Matches(message).Cast<Match>())
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

                if (localizedWords.TryGetValue(wordLower, out string localizedWord))
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
}
