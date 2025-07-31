using Bloodcraft.Resources;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using System;
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
    struct Word
    {
        public string Original { get; set; }
        public string Translation { get; set; }
    }
    struct LocalizationFile
    {
        public Code[] Codes { get; set; }
        public Node[] Nodes { get; set; }
        public Word[] Words { get; set; }
    }
    class MessageFile
    {
        public Dictionary<string, string> Messages { get; set; } = new();
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
        {PrefabGUIDs.CHAR_Winter_Yeti_VBlood_GateBoss_Major, "Primal Terrorclaw"},
        {PrefabGUIDs.FakeItem_AnyFish, "Go Fish!" }
    };

    static readonly string _pluginLanguage = ConfigService.PluginLanguage;
    static readonly Dictionary<string, string> _localizedMessagePaths = new()
    {
        {"English", "Bloodcraft.Resources.Localization.Messages.English.json"},
        {"Spanish", "Bloodcraft.Resources.Localization.Messages.Spanish.json"}
    };

    static readonly Dictionary<uint, string> _messageDictionary = [];
    public LocalizationService()
    {
        InitializeLocalizations();
        InitializePrefabGuidNames();
    }
    static void InitializeLocalizations()
    {
        // LoadPrefabHashesToGuidStrings();
        LoadGuidStringsToLocalizedNames();
        LoadLocalizedMessages();
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
    static void LoadGuidStringsToLocalizedNames()
    {
        string resourceName = _localizedLanguages.ContainsKey(_language) ? _localizedLanguages[_language] : "Bloodcraft.Resources.Localization.English.json";
        Assembly assembly = Assembly.GetExecutingAssembly();

        // Plugin.LogInstance.LogInfo($"[Localization] Trying to load resource - {resourceName}");

        Stream stream = assembly.GetManifestResourceStream(resourceName);

        if (stream == null)
        {
            Plugin.LogInstance.LogError($"[Localization] Failed to load resource - {resourceName}");
        }

        using StreamReader localizationReader = new(stream);

        string jsonContent = localizationReader.ReadToEnd();

        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            Plugin.LogInstance.LogError($"[Localization] No JSON content!");
        }

        var localizationFile = JsonSerializer.Deserialize<LocalizationFile>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (localizationFile.Nodes == null)
        {
            Plugin.LogInstance.LogError($"[Localization] Deserialized file is null or missing Nodes!");
        }

        localizationFile.Nodes
            .ToDictionary(x => x.Guid, x => x.Text)
            .ForEach(kvp => _guidStringsToLocalizedNames[kvp.Key] = kvp.Value);
    }

    static void LoadLocalizedMessages()
    {
        const string englishResource = "Bloodcraft.Resources.Localization.Messages.English.json";
        Assembly assembly = Assembly.GetExecutingAssembly();

        using Stream streamEng = assembly.GetManifestResourceStream(englishResource);
        if (streamEng == null) return;

        string englishJson = new StreamReader(streamEng).ReadToEnd();
        var englishFile = JsonSerializer.Deserialize<MessageFile>(englishJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (englishFile?.Messages == null) return;

        Dictionary<string, string> localized = englishFile.Messages;
        if (_localizedMessagePaths.TryGetValue(_pluginLanguage, out string localizedResource) && _pluginLanguage != "English")
        {
            using Stream streamLoc = assembly.GetManifestResourceStream(localizedResource);
            if (streamLoc != null)
            {
                string locJson = new StreamReader(streamLoc).ReadToEnd();
                var locFile = JsonSerializer.Deserialize<MessageFile>(locJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (locFile?.Messages != null)
                {
                    localized = locFile.Messages;
                }
            }
        }

        foreach (var kvp in englishFile.Messages)
        {
            string engText = kvp.Value;
            string text = localized.ContainsKey(kvp.Key) ? localized[kvp.Key] : engText;
            uint hash = ComputeHash(engText);
            _messageDictionary[hash] = text;
        }
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

    public static void Reply(ChatCommandContext ctx, string englishText, params object[] args)
    {
        uint hash = ComputeHash(englishText);
        string message = englishText;

        if (_messageDictionary.TryGetValue(hash, out string localized))
        {
            message = args.Length > 0 ? string.Format(localized, args) : localized;
        }
        else if (args.Length > 0)
        {
            message = string.Format(englishText, args);
        }

        ctx.Reply(message);
    }
    [Obsolete("Use Reply instead")]
    public static void HandleReply(ChatCommandContext ctx, string message)
    {
        Reply(ctx, message);
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

}
