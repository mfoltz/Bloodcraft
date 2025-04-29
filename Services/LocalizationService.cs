using Bloodcraft.Resources;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using System.Reflection;
using System.Text.Json;
using Unity.Collections;
using Unity.Entities;
using VampireCommandFramework;
using static Bloodcraft.Resources.PrefabNames;

namespace Bloodcraft.Services;
internal class LocalizationService // the bones are from KindredCommands, ty Odjit c:
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
        {"English", "Bloodcraft.Resources.Localization.English.json"},
        {"German", "Bloodcraft.Resources.Localization.German.json"},
        {"French", "Bloodcraft.Resources.Localization.French.json"},
        {"Spanish", "Bloodcraft.Resources.Localization.Spanish.json"},
        {"Italian", "Bloodcraft.Resources.Localization.Italian.json"},
        {"Japanese", "Bloodcraft.Resources.Localization.Japanese.json"},
        {"Koreana", "Bloodcraft.Resources.Localization.Koreana.json"},
        {"Portuguese", "Bloodcraft.Resources.Localization.Portuguese.json"},
        {"Russian", "Bloodcraft.Resources.Localization.Russian.json"},
        {"SimplifiedChinese", "Bloodcraft.Resources.Localization.SChinese.json"},
        {"TraditionalChinese", "Bloodcraft.Resources.Localization.TChinese.json"},
        {"Hungarian", "Bloodcraft.Resources.Localization.Hungarian.json"},
        {"Latam", "Bloodcraft.Resources.Localization.Latam.json"},
        {"Polish", "Bloodcraft.Resources.Localization.Polish.json"},
        {"Thai", "Bloodcraft.Resources.Localization.Thai.json"},
        {"Turkish", "Bloodcraft.Resources.Localization.Turkish.json"},
        {"Vietnamese", "Bloodcraft.Resources.Localization.Vietnamese.json"},
        {"Brazilian", "Bloodcraft.Resources.Localization.Brazilian.json"}
    };

    static readonly Dictionary<int, string> _guidHashesToGuidStrings = [];
    static readonly Dictionary<string, string> _guidStringsToLocalizedNames = [];
    public static IReadOnlyDictionary<PrefabGUID, string> PrefabGuidNames => _prefabGuidNames;
    static readonly Dictionary<PrefabGUID, string> _prefabGuidNames = [];

    static readonly Dictionary<PrefabGUID, string> _prefabGuidNameOverrides = new() // based off of converter (FoundPrimal) from KindredCommands, will add other names over time for villagers and others maybe >_>
    {
        {PrefabGUIDs.CHAR_Bandit_Chaosarrow_GateBoss_Minor, "Primal Lidia"},
        {PrefabGUIDs.CHAR_Bandit_Foreman_VBlood_GateBoss_Minor, "Primal Rufus"},
        {PrefabGUIDs.CHAR_Bandit_StoneBreaker_VBlood_GateBoss_Minor, "Primal Errol"},
        {PrefabGUIDs.CHAR_Bandit_Tourok_GateBoss_Minor, "Primal Quincey"},
        {PrefabGUIDs.CHAR_Frostarrow_GateBoss_Minor, "Primal Keely"},
        {PrefabGUIDs.CHAR_Gloomrot_Purifier_VBlood_GateBoss_Major, "Primal Angram"},
        {PrefabGUIDs.CHAR_Gloomrot_Voltage_VBlood_GateBoss_Major, "Primal Domina"},
        {PrefabGUIDs.CHAR_Militia_Guard_VBlood_GateBoss_Minor, "Primal Vincent"},
        {PrefabGUIDs.CHAR_Militia_Leader_VBlood_GateBoss_Major, "Primal Octavian"},
        {PrefabGUIDs.CHAR_Poloma_VBlood_GateBoss_Minor, "Primal Poloma"},
        {PrefabGUIDs.CHAR_Spider_Queen_VBlood_GateBoss_Major, "Primal Ungora"},
        {PrefabGUIDs.CHAR_Undead_BishopOfDeath_VBlood_GateBoss_Minor, "Primal Goreswine"},
        {PrefabGUIDs.CHAR_Undead_BishopOfShadows_VBlood_GateBoss_Major, "Primal Leandra"},
        {PrefabGUIDs.CHAR_Undead_Infiltrator_VBlood_GateBoss_Major, "Primal Bane"},
        {PrefabGUIDs.CHAR_Undead_Leader_Vblood_GateBoss_Minor, "Primal Kriig"},
        {PrefabGUIDs.CHAR_Undead_ZealousCultist_VBlood_GateBoss_Major, "Primal Foulrot"},
        {PrefabGUIDs.CHAR_VHunter_Jade_VBlood_GateBoss_Major, "Primal Jade"},
        {PrefabGUIDs.CHAR_VHunter_Leader_GateBoss_Minor, "Primal Tristan"},
        {PrefabGUIDs.CHAR_Villager_CursedWanderer_VBlood_GateBoss_Major, "Primal Ben"},
        {PrefabGUIDs.CHAR_Wendigo_GateBoss_Major, "Primal Frostmaw"},
        {PrefabGUIDs.CHAR_WerewolfChieftain_VBlood_GateBoss_Major, "Primal Willfred"},
        {PrefabGUIDs.CHAR_Winter_Yeti_VBlood_GateBoss_Major, "Primal Terrorclaw"}
    };
    public LocalizationService()
    {
        InitializeLocalizations();
        InitializePrefabGuidNames();
    }
    static void InitializeLocalizations()
    {
        // LoadPrefabHashesToGuidStrings();
        LoadGuidStringsToLocalizedNames();
    }
    static void InitializePrefabGuidNames()
    {
        var namesToPrefabGuids = Core.SystemService.PrefabCollectionSystem._PrefabDataLookup;

        var prefabGuids = namesToPrefabGuids.GetKeyArray(Allocator.Temp);
        var assetData = namesToPrefabGuids.GetValueArray(Allocator.Temp);

        try
        {
            for (int i = 0; i < prefabGuids.Length; i++)
            {
                var prefabGuid = prefabGuids[i];
                var assetDataValue = assetData[i];

                _prefabGuidNames[prefabGuid] = assetDataValue.AssetName.Value;
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogWarning($"Error initializing prefab names: {ex.Message}");
        }
        finally
        {
            // Core.Log.LogWarning($"[LocalizationService] Prefab names initialized - {_prefabGuidsToNames.Count}");
            prefabGuids.Dispose();
            assetData.Dispose();
        }
    }

    /*
    static void LoadPrefabHashesToGuidStrings()
    {
        string resourceName = "Bloodcraft.Resources.Localization.PrefabNames.json";
        Assembly assembly = Assembly.GetExecutingAssembly();

        Stream stream = assembly.GetManifestResourceStream(resourceName);
        using StreamReader reader = new(stream);

        string jsonContent = reader.ReadToEnd();
        var prefabNames = JsonSerializer.Deserialize<Dictionary<int, string>>(jsonContent);

        prefabNames
            .ForEach(kvp => _guidHashesToGuidStrings[kvp.Key] = kvp.Value);
    }
    */
    static void LoadGuidStringsToLocalizedNames()
    {
        string resourceName = _localizedLanguages.ContainsKey(_language) ? _localizedLanguages[_language] : "Bloodcraft.Resources.Localization.English.json";
        Assembly assembly = Assembly.GetExecutingAssembly();

        Stream stream = assembly.GetManifestResourceStream(resourceName);
        using StreamReader localizationReader = new(stream);

        string jsonContent = localizationReader.ReadToEnd();
        var localizationFile = JsonSerializer.Deserialize<LocalizationFile>(jsonContent);

        localizationFile.Nodes
            .ToDictionary(x => x.Guid, x => x.Text)
            .ForEach(kvp => _guidStringsToLocalizedNames[kvp.Key] = kvp.Value);
    }
    public static string GetAssetGuidString(PrefabGUID prefabGUID)
    {
        if (_guidHashesToGuidStrings.TryGetValue(prefabGUID.GuidHash, out var guidString))
        {
            return guidString;
        }

        return string.Empty;
    }
    public static string GetGuidString(PrefabGUID prefabGuid)
    {
        if (LocalizedNameKeys.TryGetValue(prefabGuid, out string guidString))
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
    public static string GetNameFromPrefabGuid(PrefabGUID prefabGuid)
    {
        if (_prefabGuidNameOverrides.TryGetValue(prefabGuid, out string characterName)) return characterName;
        else return GetNameFromGuidString(GetGuidString(prefabGuid));
    }
    public static void HandleReply(ChatCommandContext ctx, string message)
    {
        ctx.Reply(message);
    }

    static readonly ComponentType[] _networkEventComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<FromCharacter>()),
        ComponentType.ReadOnly(Il2CppType.Of<NetworkEventType>()),
        ComponentType.ReadOnly(Il2CppType.Of<SendNetworkEventTag>()),
        ComponentType.ReadOnly(Il2CppType.Of<ChatMessageServerEvent>())
    ];

    static readonly NetworkEventType _networkEventType = new()
    {
        IsAdminEvent = false,
        EventId = NetworkEvents.EventId_ChatMessageServerEvent,
        IsDebugEvent = false,
    };
    public static void SendToClient(Entity playerCharacter, Entity userEntity, string messageWithMAC) // for later maybe also should be moved >_>
    {
        ChatMessageServerEvent chatMessageEvent = new()
        {
            MessageText = new FixedString512Bytes(messageWithMAC),
            MessageType = ServerChatMessageType.System,
            FromCharacter = playerCharacter.GetNetworkId(),
            FromUser = userEntity.GetNetworkId(),
            TimeUTC = DateTime.UtcNow.Ticks
        };

        Entity networkEntity = Core.EntityManager.CreateEntity(_networkEventComponents);
        networkEntity.Write(new FromCharacter { Character = playerCharacter, User = userEntity });
        networkEntity.Write(_networkEventType);
        networkEntity.Write(chatMessageEvent);
    }
    public static void HandleServerReply(EntityManager entityManager, User user, string message)
    {
        FixedString512Bytes fixedMessage = new(message);
        ServerChatUtils.SendSystemMessageToClient(entityManager, user, ref fixedMessage);
    }

    /* this works fine for languages without heavy reliance on order of phrases, at some point will refactor responses to use strings from user-made language file but until then this is best bet
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
