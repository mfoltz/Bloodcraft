﻿using Bloodcraft.Interfaces;
using Bloodcraft.Resources;
using Bloodcraft.Services;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using System.Diagnostics;
using System.Text;
using Unity.Entities;
using VampireCommandFramework;
using static Bloodcraft.Systems.Expertise.WeaponManager;
using static Bloodcraft.Utilities.Misc.PlayerBools;
using WeaponType = Bloodcraft.Interfaces.WeaponType;

namespace Bloodcraft.Utilities;
internal static class Misc
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static GameDataSystem GameDataSystem => SystemService.GameDataSystem;

    static readonly Random _random = new();
    const string STAT_MOD = "StatMod";
    public enum SpellSchool : int
    {
        Shadow = 0,
        Blood = 1,
        Chaos = 2,
        Unholy = 3,
        Illusion = 4,
        Frost = 5,
        Storm = 6
    }
    public class BiDictionary<T1, T2> : IEnumerable<KeyValuePair<T1, T2>> // kind of weird but idk it can stay for now
    {
        readonly Dictionary<T1, T2> _forward = [];
        readonly Dictionary<T2, T1> _reverse = [];
        public BiDictionary() { }
        public BiDictionary(IEnumerable<KeyValuePair<T1, T2>> pairs)
        {
            foreach (var (key, value) in pairs)
            {
                Add(key, value);
            }
        }
        public void Add(T1 key, T2 value)
        {
            _forward[key] = value;
            _reverse[value] = key;
        }
        public T2 this[T1 key] => _forward[key];
        public T1 this[T2 key] => _reverse[key];
        public bool TryGetByFirst(T1 key, out T2 value) => _forward.TryGetValue(key, out value);
        public bool TryGetBySecond(T2 key, out T1 value) => _reverse.TryGetValue(key, out value);
        public IEnumerable<T1> Keys => _forward.Keys;
        public IEnumerable<T2> Values => _forward.Values;
        public IEnumerator<KeyValuePair<T1, T2>> GetEnumerator() => _forward.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
    public static class SpellSchoolInfusionMap
    {
        public static readonly BiDictionary<SpellSchool, PrefabGUID> SpellSchoolInfusions = [];
        static SpellSchoolInfusionMap()
        {
            SpellSchoolInfusions.Add(SpellSchool.Blood, PrefabGUIDs.SpellMod_Weapon_BloodInfused);
            SpellSchoolInfusions.Add(SpellSchool.Chaos, PrefabGUIDs.SpellMod_Weapon_ChaosInfused);
            SpellSchoolInfusions.Add(SpellSchool.Shadow, PrefabGUIDs.SpellMod_Weapon_UndeadInfused);
            SpellSchoolInfusions.Add(SpellSchool.Illusion, PrefabGUIDs.SpellMod_Weapon_IllusionInfused);
            SpellSchoolInfusions.Add(SpellSchool.Frost, PrefabGUIDs.SpellMod_Weapon_FrostInfused);
            SpellSchoolInfusions.Add(SpellSchool.Storm, PrefabGUIDs.SpellMod_Weapon_StormInfused);
        }
    }

    static readonly Dictionary<PrefabGUID, PrefabGUID> _infusionShinyBuffs = new()
    {
        { PrefabGUIDs.SpellMod_Weapon_BloodInfused, new(348724578) },     // ignite 
        { PrefabGUIDs.SpellMod_Weapon_ChaosInfused, new (-1576512627) },  // static
        { PrefabGUIDs.SpellMod_Weapon_UndeadInfused, new (-1246704569) }, // leech
        { PrefabGUIDs.SpellMod_Weapon_IllusionInfused, new(1723455773) }, // weaken
        { PrefabGUIDs.SpellMod_Weapon_FrostInfused, new(27300215) },      // chill
        { PrefabGUIDs.SpellMod_Weapon_StormInfused, new(-325758519) }     // condemn
    };
    public static IReadOnlyDictionary<PrefabGUID, PrefabGUID> InfusionShinyBuffs => _infusionShinyBuffs;
    public enum ScrollingTextMessage
    {
        PlayerExperience,
        PlayerExpertise,
        PlayerLegacy,
        FamiliarExperience,
        ProfessionExperience,
        ProfessionYields
    }

    const string SCT_PLAYER_LVL = "PlayerXP";
    const string SCT_PLAYER_WEP = "ExpertiseXP";
    const string SCT_PLAYER_BL = "LegacyXP";
    const string SCT_FAMILIAR_LVL = "FamiliarXP";
    const string SCT_PROFESSIONS = "ProfessionXP";
    const string SCT_YIELD = "ProfessionYields";

    public static readonly List<string> ScrollingTextNames =
    [
        SCT_PLAYER_LVL,
        SCT_PLAYER_WEP,
        SCT_PLAYER_BL,
        SCT_FAMILIAR_LVL,
        SCT_PROFESSIONS,
        SCT_YIELD
    ];

    public static readonly Dictionary<string, ScrollingTextMessage> ScrollingTextNameMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { SCT_PLAYER_LVL, ScrollingTextMessage.PlayerExperience },
        { SCT_PLAYER_WEP, ScrollingTextMessage.PlayerExpertise },
        { SCT_PLAYER_BL, ScrollingTextMessage.PlayerLegacy },
        { SCT_FAMILIAR_LVL, ScrollingTextMessage.FamiliarExperience },
        { SCT_PROFESSIONS, ScrollingTextMessage.ProfessionExperience },
        { SCT_YIELD, ScrollingTextMessage.ProfessionYields }
    };

    public static readonly Dictionary<ScrollingTextMessage, string> ScrollingTextBoolKeyMap = new()
    {
        { ScrollingTextMessage.PlayerExperience, SCT_PLAYER_LVL_KEY },
        { ScrollingTextMessage.PlayerExpertise, SCT_PLAYER_WEP_KEY },
        { ScrollingTextMessage.PlayerLegacy, SCT_PLAYER_BL_KEY },
        { ScrollingTextMessage.FamiliarExperience, SCT_FAMILIAR_LVL_KEY },
        { ScrollingTextMessage.ProfessionExperience, SCT_PROFESSIONS_KEY },
        { ScrollingTextMessage.ProfessionYields, SCT_YIELD_KEY }
    };
    public static class PlayerBools
    {
        public const string EXPERIENCE_LOG_KEY = "ExperienceLogging";
        public const string QUEST_LOG_KEY = "QuestLogging";
        public const string PROFESSION_LOG_KEY = "ProfessionLogging";
        public const string WEAPON_LOG_KEY = "WeaponLogging";
        public const string BLOOD_LOG_KEY = "BloodLogging";
        public const string SPELL_LOCK_KEY = "SpellLock";
        public const string SHIFT_LOCK_KEY = "ShiftLock";
        public const string PARTY_INVITE_KEY = "PartyInvite";
        public const string EMOTE_ACTIONS_KEY = "EmoteActions";
        public const string STARTER_KIT_KEY = "StarterKit";
        public const string VBLOOD_EMOTES_KEY = "VBloodEmotes";
        public const string SHINY_FAMILIARS_KEY = "ShinyFamiliars";
        public const string REMINDERS_KEY = "Reminders";
        public const string SCT_PLAYER_LVL_KEY = "PlayerExperienceSCT";
        public const string SCT_PLAYER_WEP_KEY = "ExpertiseSCT";
        public const string SCT_PLAYER_BL_KEY = "LegacySCT";
        public const string SCT_FAMILIAR_LVL_KEY = "FamiliarExperienceSCT";
        public const string SCT_PROFESSIONS_KEY = "ProfessionExperienceSCT";
        public const string SCT_YIELD_KEY = "ProfessionYieldSCT";
        public const string SHAPESHIFT_KEY = "Shapeshift";
        public const string SHROUD_KEY = "Shroud";
        public const string CLASS_BUFFS_KEY = "Passives";
        public const string PRESTIGE_BUFFS_KEY = "PrestigeBuffs";

        public const string WORKER_KEY = nameof(BloodType.Worker);
        public const string WARRIOR_KEY = nameof(BloodType.Warrior);
        public const string SCHOLAR_KEY = nameof(BloodType.Scholar);
        public const string ROGUE_KEY = nameof(BloodType.Rogue);
        public const string MUTANT_KEY = nameof(BloodType.Mutant);
        public const string VBLOOD_KEY = nameof(BloodType.VBlood);
        public const string NONE_BLOOD_KEY = nameof(BloodType.None);
        public const string GATE_BOSS_KEY = nameof(BloodType.GateBoss);
        public const string DRACULIN_KEY = nameof(BloodType.Draculin);
        public const string IMMORTAL_KEY = nameof(BloodType.Immortal);
        public const string CREATURE_KEY = nameof(BloodType.Creature);
        public const string BRUTE_KEY = nameof(BloodType.Brute);
        public const string CORRUPTION_KEY = nameof(BloodType.Corruption);

        public const string SWORD_KEY = nameof(WeaponType.Sword);
        public const string AXE_KEY = nameof(WeaponType.Axe);
        public const string MACE_KEY = nameof(WeaponType.Mace);
        public const string SPEAR_KEY = nameof(WeaponType.Spear);
        public const string CROSSBOW_KEY = nameof(WeaponType.Crossbow);
        public const string GREATSWORD_KEY = nameof(WeaponType.GreatSword);
        public const string SLASHERS_KEY = nameof(WeaponType.Slashers);
        public const string PISTOLS_KEY = nameof(WeaponType.Pistols);
        public const string REAPER_KEY = nameof(WeaponType.Reaper);
        public const string LONGBOW_KEY = nameof(WeaponType.Longbow);
        public const string WHIP_KEY = nameof(WeaponType.Whip);
        public const string UNARMED_KEY = nameof(WeaponType.Unarmed);
        public const string FISHING_POLE_KEY = nameof(WeaponType.FishingPole);
        public const string TWIN_BLADES_KEY = nameof(WeaponType.TwinBlades);
        public const string DAGGERS_KEY = nameof(WeaponType.Daggers);
        public const string CLAWS_KEY = nameof(WeaponType.Claws);

        public const string BAG_HEAVIER_MESSAGE = "Your bag feels slightly heavier...";
        public const string ITEM_DROPPED_MESSAGE = "Something fell out of your bag!";

        public static readonly Dictionary<string, bool> DefaultBools = new()
        {
            [EXPERIENCE_LOG_KEY] = false,
            [QUEST_LOG_KEY] = true,
            [PROFESSION_LOG_KEY] = false,
            [WEAPON_LOG_KEY] = false,
            [BLOOD_LOG_KEY] = false,
            [SPELL_LOCK_KEY] = false,
            [SHIFT_LOCK_KEY] = false,
            [PARTY_INVITE_KEY] = false,
            [EMOTE_ACTIONS_KEY] = false,
            [STARTER_KIT_KEY] = false,
            [VBLOOD_EMOTES_KEY] = true,
            [SHINY_FAMILIARS_KEY] = true,
            [REMINDERS_KEY] = true,
            [SCT_PLAYER_LVL_KEY] = true,
            [SCT_PLAYER_WEP_KEY] = true,
            [SCT_PLAYER_BL_KEY] = true,
            [SCT_FAMILIAR_LVL_KEY] = true,
            [SCT_PROFESSIONS_KEY] = true,
            [SCT_YIELD_KEY] = true,
            [SHAPESHIFT_KEY] = false,
            [SHROUD_KEY] = true,
            [CLASS_BUFFS_KEY] = false,
            [PRESTIGE_BUFFS_KEY] = true,

            [WORKER_KEY] = true,
            [WARRIOR_KEY] = true,
            [SCHOLAR_KEY] = true,
            [ROGUE_KEY] = true,
            [MUTANT_KEY] = true,
            [VBLOOD_KEY] = true,
            [NONE_BLOOD_KEY] = true,
            [GATE_BOSS_KEY] = true,
            [DRACULIN_KEY] = true,
            [IMMORTAL_KEY] = true,
            [CREATURE_KEY] = true,
            [BRUTE_KEY] = true,
            [CORRUPTION_KEY] = true,

            [SWORD_KEY] = true,
            [AXE_KEY] = true,
            [MACE_KEY] = true,
            [SPEAR_KEY] = true,
            [CROSSBOW_KEY] = true,
            [GREATSWORD_KEY] = true,
            [SLASHERS_KEY] = true,
            [PISTOLS_KEY] = true,
            [REAPER_KEY] = true,
            [LONGBOW_KEY] = true,
            [WHIP_KEY] = true,
            [UNARMED_KEY] = true,
            [FISHING_POLE_KEY] = true,
            [TWIN_BLADES_KEY] = true,
            [DAGGERS_KEY] = true,
            [CLAWS_KEY] = true
        };
        public static bool GetPlayerBool(ulong steamId, string boolKey)
        {
            var bools = DataService.PlayerBoolsManager.LoadPlayerBools(steamId);

            if (bools.TryGetValue(boolKey, out bool value))
            {
                return value;
            }
            else if (DefaultBools.TryGetValue(boolKey, out bool defaultValue))
            {
                bools[boolKey] = defaultValue;
                DataService.PlayerBoolsManager.SavePlayerBools(steamId, bools);

                return defaultValue;
            }

            return false;
        }
        public static void SetPlayerBool(ulong steamId, string boolKey, bool value)
        {
            var bools = DataService.PlayerBoolsManager.LoadPlayerBools(steamId);
            bools[boolKey] = value;

            DataService.PlayerBoolsManager.SavePlayerBools(steamId, bools);
        }
        public static void TogglePlayerBool(ulong steamId, string boolKey)
        {
            var bools = DataService.PlayerBoolsManager.LoadPlayerBools(steamId);

            if (bools.ContainsKey(boolKey))
            {
                bools[boolKey] = !bools[boolKey];
            }
            else
            {
                bools[boolKey] = !DefaultBools.TryGetValue(boolKey, out var defaultValue) || !defaultValue;
            }

            DataService.PlayerBoolsManager.SavePlayerBools(steamId, bools);
        }
        public static bool TryGetPlayerBool(ulong steamId, string boolKey, out bool value)
        {
            var bools = DataService.PlayerBoolsManager.LoadPlayerBools(steamId);

            if (bools.TryGetValue(boolKey, out value))
            {
                return true;
            }
            else if (DefaultBools.TryGetValue(boolKey, out var defaultValue))
            {
                value = defaultValue;

                bools[boolKey] = defaultValue;
                DataService.PlayerBoolsManager.SavePlayerBools(steamId, bools);

                return true;
            }

            value = false;
            return false;
        }
        public static string GetEnabledDisabledBool(ulong steamId, string boolKey)
        {
            return GetPlayerBool(steamId, boolKey) ? "<color=green>enabled</color>" : "<color=red>disabled</color>";
        }
    }
    public static string FormatTimespan(TimeSpan timeSpan)
    {
        string timeString = timeSpan.ToString(@"mm\:ss");
        return timeString;
    }
    public static string FormatPercentStatValue(float value)
    {
        string bonusString = (value * 100).ToString("F0") + "%";
        return bonusString;
    }

    static readonly Dictionary<PrefabGUID, float> _statModPresetValues = new()
    {
        { new(-1545133628), 0.25f },
        { new(1448170922), 0.15f },
        { new(-1700712765), 0.25f },
        { new(523084427), 0.15f },
        { new(1179205309), 0.15f },
        { new(-2004879548), 0.10f }, 
        { new(539854831), 0.15f },
        { new(-1274939577), 0.10f },
        { new(1032018140), 0.15f },
        { new(1842448780), 0.15f }
    };
    public static bool TryGetStatTypeFromPrefabName(PrefabGUID prefabGuid, float originalValue, out UnitStatType statType, out float resolvedValue)
    {
        statType = default;
        resolvedValue = originalValue;

        string rawPrefabString = prefabGuid.GetPrefabName();
        string baseName = rawPrefabString.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (baseName is null) return false;

        baseName = baseName.Replace("StatMod_", "", StringComparison.OrdinalIgnoreCase)
                           .Replace("Unique_", "", StringComparison.OrdinalIgnoreCase);

        string[] tierSuffixes = ["_Low", "_Mid", "_High"];
        foreach (var suffix in tierSuffixes)
        {
            if (baseName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                baseName = baseName[..^suffix.Length];
                break;
            }
        }

        if (_statModPresetValues.TryGetValue(prefabGuid, out float presetValue))
        {
            resolvedValue = presetValue;
        }

        if (Enum.TryParse(baseName, ignoreCase: true, out statType)) return true;

        switch (baseName.ToLowerInvariant())
        {
            case "attackspeed":
                statType = UnitStatType.PrimaryAttackSpeed;
                return true;
            case "criticalstrikephysical":
                statType = UnitStatType.PhysicalCriticalStrikeChance;
                return true;
            case "criticalstrikephysicalpower":
                statType = UnitStatType.PhysicalCriticalStrikeDamage;
                return true;
            case "criticalstrikespellpower":
                statType = UnitStatType.SpellCriticalStrikeDamage;
                return true;
            case "criticalstrikespells":
                statType = UnitStatType.SpellCriticalStrikeChance;
                return true;
            case "criticalstrikespell":
                statType = UnitStatType.SpellCriticalStrikeChance;
                return true;
            case "spellcooldownreduction":
                statType = UnitStatType.SpellCooldownRecoveryRate;
                return true;
            case "weaponcooldownreduction":
                statType = UnitStatType.WeaponCooldownRecoveryRate;
                return true;
            case "spellleech":
                statType = UnitStatType.SpellLifeLeech;
                return true;
        }

        Core.Log.LogWarning($"Unmapped stat mod prefab: '{rawPrefabString}' → parsed '{baseName}'");
        return false;
    }

    /*
    public static bool TryGetStatTypeFromPrefabName(string rawPrefabString, out UnitStatType statType)
    {
        statType = default;

        if (string.IsNullOrWhiteSpace(rawPrefabString))
            return false;

        // Step 1: Extract the filename portion (remove 'PrefabGuid(...)' etc.)
        string baseName = rawPrefabString.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (baseName is null)
            return false;

        // Step 2: Strip known prefixes and suffixes
        baseName = baseName.Replace("StatMod_", "", StringComparison.OrdinalIgnoreCase)
                           .Replace("Unique_", "", StringComparison.OrdinalIgnoreCase);

        // Remove suffixes like "_Low", "_Mid", "_High"
        string[] tierSuffixes = ["_Low", "_Mid", "_High"];
        foreach (var suffix in tierSuffixes)
        {
            if (baseName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                baseName = baseName[..^suffix.Length];
                break;
            }
        }

        // Step 3: Try to parse directly
        if (Enum.TryParse(baseName, ignoreCase: true, out statType))
            return true;

        // Step 4: Fallback alias matching
        switch (baseName.ToLowerInvariant())
        {
            case "attackspeed":
                statType = UnitStatType.PrimaryAttackSpeed;
                return true;
            case "criticalstrikephysical":
                statType = UnitStatType.PhysicalCriticalStrikeChance;
                return true;
            case "criticalstrikephysicalpower":
                statType = UnitStatType.PhysicalCriticalStrikeDamage;
                return true;
            case "criticalstrikespellpower":
                statType = UnitStatType.SpellCriticalStrikeDamage;
                return true;
            case "criticalstrikespells":
                statType = UnitStatType.SpellCriticalStrikeChance;
                return true;
            case "criticalstrikespell":
                statType = UnitStatType.SpellCriticalStrikeChance;
                return true;
            case "spellcooldownreduction":
                statType = UnitStatType.SpellCooldownRecoveryRate;
                return true;
            case "weaponcooldownreduction":
                statType = UnitStatType.WeaponCooldownRecoveryRate;
                return true;
            case "spellleech":
                statType = UnitStatType.SpellLifeLeech;
                return true;
            case "vampiredamage":
                statType = UnitStatType.DamageVsVampires;
                return true;
            default:
                Core.Log.LogWarning($"Unmapped stat mod prefab! ('{rawPrefabString}' → parsed '{baseName}')");
                return false;
        }
    }
    */
    public static string FormatWeaponStatValue(WeaponStats.WeaponStatType statType, float value)
    {
        string formattedBonus = WeaponStats.WeaponStatFormats[statType] switch
        {
            "integer" => ((int)value).ToString(),
            "decimal" => value.ToString("F2"),
            "percentage" => (value * 100).ToString("F1") + "%",
            _ => value.ToString(),
        };

        return formattedBonus;
    }
    public static void ReplySCTDetails(ChatCommandContext ctx)
    {
        ulong steamId = ctx.User.PlatformId;

        StringBuilder sb = new();
        sb.AppendLine("<color=#FFC0CB>SCT Options</color>:");

        int index = 0;
        foreach (var entry in ScrollingTextNameMap)
        {
            string name = entry.Key;
            ScrollingTextMessage message = entry.Value;

            if (ScrollingTextBoolKeyMap.TryGetValue(message, out string boolKey))
            {
                string status = GetEnabledDisabledBool(steamId, boolKey);
                sb.AppendLine($"<color=yellow>{++index}</color>| <color=white>{name}</color> ({status})");
            }
        }

        LocalizationService.Reply(ctx, sb.ToString());
    }
    public static void GiveOrDropItem(User user, Entity playerCharacter, PrefabGUID itemType, int amount)
    {
        var itemDataHashMap = GameDataSystem.ItemHashLookupMap;
        bool hasSpace = InventoryUtilities.HasFreeStackSpaceOfType(EntityManager, playerCharacter, itemDataHashMap, itemType, amount);

        if (hasSpace && ServerGameManager.TryAddInventoryItem(playerCharacter, itemType, amount))
        {
            LocalizationService.Reply(EntityManager, user, BAG_HEAVIER_MESSAGE);
        }
        else
        {
            InventoryUtilitiesServer.CreateDropItem(EntityManager, playerCharacter, itemType, amount, new Entity()); // does this create multiple drops to account for excessive stacks? noting for later
            LocalizationService.Reply(EntityManager, user, ITEM_DROPPED_MESSAGE);
        }
    }
    public static bool RollForChance(float chance)
    {
        return _random.NextDouble() < chance;
    }
    public static class Performance
    {
        static readonly Stopwatch _stopwatch = new();
        static string _label = "";
        static long _totalElapsedTicks = 0;
        public static void Start(string label)
        {
            _label = label;
            _stopwatch.Restart();
            Core.Log.LogInfo($"[TIMER] Start - {_label}");
        }
        public static void Stop()
        {
            _stopwatch.Stop();

            long elapsedTicks = _stopwatch.ElapsedTicks;
            _totalElapsedTicks += elapsedTicks;

            double elapsedMilliseconds = _stopwatch.Elapsed.TotalMilliseconds;

            Core.Log.LogInfo($"[TIMER] Stop - {_label} ({_totalElapsedTicks}t | {elapsedMilliseconds:F3}ms)");

            // Reset
            _totalElapsedTicks = 0;
        }
    }

    /*
    public static class PerformanceTimer
    {
        class TimerData
        {
            public Stopwatch Stopwatch = new();
            public long TotalElapsedTicks = 0;
        }

        static readonly Dictionary<string, TimerData> _timers = [];
        public static void Start(string label)
        {
            if (!_timers.TryGetValue(label, out var timerData))
            {
                timerData = new TimerData();
                _timers[label] = timerData;
            }

            timerData.Stopwatch.Restart();
            Core.Log.LogInfo($"[TIMER] Start - {label}");
        }
        public static void Stop(string label)
        {
            if (!_timers.TryGetValue(label, out var timerData))
            {
                Core.Log.LogWarning($"[TIMER] Attempted to stop unknown timer: {label}");
                return;
            }

            timerData.Stopwatch.Stop();
            long elapsedTicks = timerData.Stopwatch.ElapsedTicks;
            timerData.TotalElapsedTicks += elapsedTicks;
            double elapsedMilliseconds = timerData.Stopwatch.Elapsed.TotalMilliseconds;

            Core.Log.LogInfo($"[TIMER] Stop - {label} ({timerData.TotalElapsedTicks}t | {elapsedMilliseconds:F3}ms)");
        }
        public static void Reset(string label)
        {
            _timers.Remove(label);
        }
        public static void ResetAll()
        {
            _timers.Clear();
        }
    }
    */

    /*
    public static bool EarnedPermaShroud()
    {
        if (UpdateBuffsBufferDestroyPatch.PrestigeBuffs.Contains(ShroudBuff) && !character.HasBuff(ShroudBuff)
    && steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(PrestigeType.Experience, out var experiencePrestiges) && experiencePrestiges > UpdateBuffsBufferDestroyPatch.PrestigeBuffs.IndexOf(ShroudBuff))
        {
            BuffUtilities.ApplyPermanentBuff(character, ShroudBuff);
        }
    }
    */
}
