using Bloodcraft.Interfaces;
using Bloodcraft.Resources;
using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using System.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static Bloodcraft.Patches.DeathEventListenerSystemPatch;
using static Bloodcraft.Utilities.Misc.PlayerBoolsManager;
using static Bloodcraft.Utilities.Progression;

namespace Bloodcraft.Systems.Legacies;
internal static class BloodSystem
{
    static EntityManager EntityManager => Core.EntityManager;
    static SystemService SystemService => Core.SystemService;
    static EndSimulationEntityCommandBufferSystem EndSimulationEntityCommandBufferSystem => SystemService.EndSimulationEntityCommandBufferSystem;

    static readonly int _maxBloodLevel = ConfigService.MaxBloodLevel;
    static readonly int _legacyStatChoices = ConfigService.LegacyStatChoices;
    static readonly float _vBloodLegacyMultiplier = ConfigService.VBloodLegacyMultiplier;
    static readonly float _unitLegacyMultiplier = ConfigService.UnitLegacyMultiplier;
    static readonly float _prestigeRatesReducer = ConfigService.PrestigeRatesReducer;
    static readonly float _prestigeRateMultiplier = ConfigService.PrestigeRateMultiplier;

    static readonly float3 _red = new(0.9f, 0f, 0.1f);
    static readonly AssetGuid _experienceAssetGuid = AssetGuid.FromString("4210316d-23d4-4274-96f5-d6f0944bd0bb");
    static readonly PrefabGUID _sctGeneric = PrefabGUIDs.SCT_Type_Generic;

    const int BASE_BLOOD_FACTOR = 10;
    const float BLOOD_TYPE_FACTOR = 3f;
    public static IReadOnlyDictionary<BloodType, Func<ulong, (bool Success, KeyValuePair<int, float> Data)>> TryGetExtensions => _tryGetExtensions;
    static readonly Dictionary<BloodType, Func<ulong, (bool Success, KeyValuePair<int, float> Data)>> _tryGetExtensions = new()
    {
        { BloodType.Worker, steamID =>
            {
                if (steamID.TryGetPlayerWorkerLegacy(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { BloodType.Warrior, steamID =>
            {
                if (steamID.TryGetPlayerWarriorLegacy(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { BloodType.Scholar, steamID =>
            {
                if (steamID.TryGetPlayerScholarLegacy(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { BloodType.Rogue, steamID =>
            {
                if (steamID.TryGetPlayerRogueLegacy(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { BloodType.Mutant, steamID =>
            {
                if (steamID.TryGetPlayerMutantLegacy(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { BloodType.Draculin, steamID =>
            {
                if (steamID.TryGetPlayerDraculinLegacy(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { BloodType.Immortal, steamID =>
            {
                if (steamID.TryGetPlayerImmortalLegacy(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { BloodType.Creature, steamID =>
            {
                if (steamID.TryGetPlayerCreatureLegacy(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { BloodType.Brute, steamID =>
            {
                if (steamID.TryGetPlayerBruteLegacy(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { BloodType.Corruption, steamID =>
            {
                if (steamID.TryGetPlayerCorruptionLegacy(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        }
    };
    public static IReadOnlyDictionary<BloodType, Action<ulong, KeyValuePair<int, float>>> SetExtensions => _setExtensions;
    static readonly Dictionary<BloodType, Action<ulong, KeyValuePair<int, float>>> _setExtensions = new()
    {
        { BloodType.Worker, (steamID, data) => steamID.SetPlayerWorkerLegacy(data) },
        { BloodType.Warrior, (steamID, data) => steamID.SetPlayerWarriorLegacy(data) },
        { BloodType.Scholar, (steamID, data) => steamID.SetPlayerScholarLegacy(data) },
        { BloodType.Rogue, (steamID, data) => steamID.SetPlayerRogueLegacy(data) },
        { BloodType.Mutant, (steamID, data) => steamID.SetPlayerMutantLegacy(data) },
        { BloodType.Draculin, (steamID, data) => steamID.SetPlayerDraculinLegacy(data) },
        { BloodType.Immortal, (steamID, data) => steamID.SetPlayerImmortalLegacy(data) },
        { BloodType.Creature, (steamID, data) => steamID.SetPlayerCreatureLegacy(data) },
        { BloodType.Brute, (steamID, data) => steamID.SetPlayerBruteLegacy(data) },
        { BloodType.Corruption, (steamID, data) => steamID.SetPlayerCorruptionLegacy(data) }
    };
    public static IReadOnlyDictionary<BloodType, PrestigeType> BloodPrestigeTypes => _bloodPrestigeTypes;
    static readonly Dictionary<BloodType, PrestigeType> _bloodPrestigeTypes = new()
    {
        { BloodType.Worker, PrestigeType.WorkerLegacy },
        { BloodType.Warrior, PrestigeType.WarriorLegacy },
        { BloodType.Scholar, PrestigeType.ScholarLegacy },
        { BloodType.Rogue, PrestigeType.RogueLegacy },
        { BloodType.Mutant, PrestigeType.MutantLegacy },
        { BloodType.Draculin, PrestigeType.DraculinLegacy },
        { BloodType.Immortal, PrestigeType.ImmortalLegacy },
        { BloodType.Creature, PrestigeType.CreatureLegacy },
        { BloodType.Brute, PrestigeType.BruteLegacy },
        { BloodType.Corruption, PrestigeType.CorruptionLegacy }
    };
    public static IReadOnlyDictionary<PrefabGUID, BloodType> BloodBuffToBloodType => _bloodBuffToBloodType;
    static readonly Dictionary<PrefabGUID, BloodType> _bloodBuffToBloodType = new()
    {
        { PrefabGUIDs.AB_BloodBuff_Worker_Tier1, BloodType.Worker },
        { PrefabGUIDs.AB_BloodBuff_Warrior_Tier1, BloodType.Warrior },
        { PrefabGUIDs.AB_BloodBuff_Scholar_Tier1, BloodType.Scholar },
        { PrefabGUIDs.AB_BloodBuff_Rogue_Tier1, BloodType.Rogue },
        { PrefabGUIDs.AB_BloodBuff_Mutant_Tier1, BloodType.Mutant },
        { PrefabGUIDs.AB_BloodBuff_Draculin_Tier1, BloodType.Draculin },
        { PrefabGUIDs.AB_BloodBuff_Dracula_Tier1, BloodType.Immortal },
        { PrefabGUIDs.AB_BloodBuff_Creature_Tier1, BloodType.Creature },
        { PrefabGUIDs.AB_BloodBuff_Brute_Tier1, BloodType.Brute },
        { PrefabGUIDs.AB_BloodBuff_Corruption_Tier1, BloodType.Corruption }
    };
    public static IReadOnlyDictionary<BloodType, PrefabGUID> BloodTypeToBloodBuff => _bloodTypeToBloodBuff;
    static readonly Dictionary<BloodType, PrefabGUID> _bloodTypeToBloodBuff = new()
    {
        { BloodType.Worker, PrefabGUIDs.AB_BloodBuff_Worker_Tier1 },
        { BloodType.Warrior, PrefabGUIDs.AB_BloodBuff_Warrior_Tier1 },
        { BloodType.Scholar, PrefabGUIDs.AB_BloodBuff_Scholar_Tier1 },
        { BloodType.Rogue, PrefabGUIDs.AB_BloodBuff_Rogue_Tier1 },
        { BloodType.Mutant, PrefabGUIDs.AB_BloodBuff_Mutant_Tier1 },
        { BloodType.Draculin, PrefabGUIDs.AB_BloodBuff_Draculin_Tier1 },
        { BloodType.Immortal, PrefabGUIDs.AB_BloodBuff_Dracula_Tier1 },
        { BloodType.Creature, PrefabGUIDs.AB_BloodBuff_Creature_Tier1 },
        { BloodType.Brute, PrefabGUIDs.AB_BloodBuff_Brute_Tier1 },
        { BloodType.Corruption, PrefabGUIDs.AB_BloodBuff_Corruption_Tier1 }
    };
    public static IReadOnlyDictionary<BloodType, PrefabGUID> BloodTypeToConsumeSource => _bloodTypeToConsumeSource;
    static readonly Dictionary<BloodType, PrefabGUID> _bloodTypeToConsumeSource = new()
    {
        { BloodType.Worker, PrefabGUIDs.CHAR_Bandit_Worker_Gatherer },
        { BloodType.Warrior, PrefabGUIDs.CHAR_Bandit_Thief },
        { BloodType.Scholar, PrefabGUIDs.CHAR_Militia_Nun },
        { BloodType.Rogue, PrefabGUIDs.CHAR_Bandit_Scout },
        { BloodType.Mutant, PrefabGUIDs.CHAR_Mutant_Spitter },
        { BloodType.Draculin, PrefabGUIDs.CHAR_Legion_NightMaiden },
        { BloodType.Creature, PrefabGUIDs.CHAR_Cursed_Wolf },
        { BloodType.Immortal, PrefabGUIDs.CHAR_Dracula_BloodSoul_Heart },
        { BloodType.Brute, PrefabGUIDs.CHAR_Militia_Heavy },
        { BloodType.Corruption, PrefabGUIDs.CHAR_Corrupted_Wolf }
    };
    public static void ProcessLegacy(DeathEventArgs deathEvent)
    {
        Entity playerCharacter = deathEvent.Source;
        Entity target = deathEvent.Target;

        if (!target.TryGetComponent(out BloodConsumeSource bloodConsumeSource)) return;

        BloodType targetBloodType = GetBloodTypeFromPrefab(bloodConsumeSource.UnitBloodType._Value);
        int unitLevel = target.GetUnitLevel();

        float bloodValue;
        float changeFactor = 1f;

        if (target.Has<VBloodConsumeSource>())
        {
            bloodValue = BASE_BLOOD_FACTOR * unitLevel * _vBloodLegacyMultiplier;
        }
        else
        {
            bloodValue = bloodConsumeSource.BloodQuality / BASE_BLOOD_FACTOR * unitLevel * _unitLegacyMultiplier;
        }

        Entity userEntity = playerCharacter.GetUserEntity();
        User user = userEntity.GetUser();
        ulong steamId = user.PlatformId;

        Blood blood = playerCharacter.Read<Blood>();
        BloodType bloodType = BloodManager.GetCurrentBloodType(blood);
        float bloodQuality = blood.Quality;

        if (bloodType.Equals(BloodType.None)) return;
        else if (targetBloodType.Equals(bloodType))
        {
            bloodValue *= BLOOD_TYPE_FACTOR; // same type multiplier
        }

        float qualityMultiplier = 1f + (bloodQuality / 100f);
        qualityMultiplier = Mathf.Min(qualityMultiplier, 2f); // Cap the multiplier at 2
        bloodValue *= qualityMultiplier;

        if (steamId.TryGetPlayerPrestiges(out var prestiges))
        {
            if (prestiges.TryGetValue(BloodPrestigeTypes[bloodType], out var bloodLegacy))
            {
                changeFactor -= (_prestigeRatesReducer * bloodLegacy);
            }

            if (prestiges.TryGetValue(PrestigeType.Experience, out var xpPrestige))
            {
                changeFactor += (_prestigeRateMultiplier * xpPrestige);
            }
        }

        bloodValue *= changeFactor;

        IBloodLegacy handler = BloodLegacyFactory.GetBloodHandler(bloodType);
        if (handler != null)
        {
            SaveBloodExperience(steamId, handler, bloodValue, out bool leveledUp, out int newLevel);
            NotifyPlayer(playerCharacter, userEntity, user, steamId, bloodType, bloodValue, leveledUp, newLevel, handler, deathEvent.ScrollingTextDelay);
        }
    }
    public static void SaveBloodExperience(ulong steamId, IBloodLegacy handler, float gainedXP, out bool leveledUp, out int newLevel)
    {
        var xpData = handler.GetLegacyData(steamId);
        int currentLevel = xpData.Key;
        float currentXP = xpData.Value;

        if (currentLevel >= _maxBloodLevel)
        {
            leveledUp = false;
            newLevel = currentLevel;
            return;
        }

        float newExperience = currentXP + gainedXP;
        newLevel = ConvertXpToLevel(newExperience);
        leveledUp = false;

        if (newLevel > currentLevel)
        {
            leveledUp = true;
            if (newLevel > _maxBloodLevel)
            {
                newLevel = _maxBloodLevel;
                newExperience = ConvertLevelToXp(_maxBloodLevel);
            }
        }

        handler.SetLegacyData(steamId, new KeyValuePair<int, float>(newLevel, newExperience));
    }
    static void HandleBloodLevelUp(User user, BloodType bloodType, int newLevel, ulong steamID)
    {
        if (newLevel <= _maxBloodLevel)
        {
            LocalizationService.HandleServerReply(EntityManager, user,
                $"<color=red>{bloodType}</color> legacy improved to [<color=white>{newLevel}</color>]!");
        }

        if (GetPlayerBool(steamID, REMINDERS_KEY))
        {
            if (steamID.TryGetPlayerBloodStats(out var bloodStats) && bloodStats.TryGetValue(bloodType, out var stats))
            {
                int currentStatCount = stats.Count;

                if (currentStatCount < _legacyStatChoices)
                {
                    int choicesLeft = _legacyStatChoices - currentStatCount;
                    string bonusString = choicesLeft > 1 ? "bonuses" : "bonus";

                    LocalizationService.HandleServerReply(EntityManager, user,
                        $"{choicesLeft} <color=white>stat</color> <color=#00FFFF>{bonusString}</color> available for <color=red>{bloodType.ToString().ToLower()}</color>; use '<color=white>.bl cst [Stat]</color>' to choose and '<color=white>.bl lst</color>' to see options. (toggle reminders with <color=white>'.misc remindme'</color>)");
                }
            }
        }
    }
    public static void NotifyPlayer(Entity playerCharacter, Entity userEntity, User user, ulong steamId, BloodType bloodType, float gainedXP, bool leveledUp, int newLevel, IBloodLegacy handler, float delay)
    {
        int gainedIntXP = (int)gainedXP;
        int levelProgress = GetLevelProgress(steamId, handler);

        if (newLevel >= _maxBloodLevel) return;
        else if (gainedXP <= 0) return;

        if (leveledUp)
        {
            HandleBloodLevelUp(user, bloodType, newLevel, steamId);
            Buffs.RefreshStats(user.LocalCharacter.GetEntityOnServer());
        }

        if (GetPlayerBool(steamId, BLOOD_LOG_KEY))
        {
            LocalizationService.HandleServerReply(EntityManager, user,
                $"+<color=yellow>{gainedIntXP}</color> <color=red>{bloodType}</color> <color=#FFC0CB>essence</color> (<color=white>{levelProgress}%</color>)");
        }

        if (GetPlayerBool(steamId, SCT_PLAYER_BL_KEY))
        {
            // Core.Log.LogInfo($"Legacy SCT for {user.CharacterName.Value} with gainedXP: {gainedXP} and delay: {delay}");
            PlayerLegacySCTDelayRoutine(playerCharacter, userEntity, _red, gainedXP, delay).Run();
        }
    }
    static IEnumerator PlayerLegacySCTDelayRoutine(Entity playerCharacter, Entity userEntity, float3 color, float gainedXP, float delay) // maybe just have one of these in progression utilities but later
    {
        yield return new WaitForSeconds(delay);

        float3 position = playerCharacter.GetPosition();
        ScrollingCombatTextMessage.Create(EntityManager, EndSimulationEntityCommandBufferSystem.CreateCommandBuffer(), _experienceAssetGuid, position, color, playerCharacter, gainedXP, _sctGeneric, userEntity);
    }
    public static int GetLevelProgress(ulong SteamID, IBloodLegacy handler)
    {
        int currentLevel = GetLevel(SteamID, handler);
        float currentXP = GetXp(SteamID, handler);

        int currentLevelXP = ConvertLevelToXp(currentLevel);
        int nextLevelXP = ConvertLevelToXp(++currentLevel);

        double neededXP = nextLevelXP - currentLevelXP;
        double earnedXP = nextLevelXP - currentXP;

        return 100 - (int)Math.Ceiling(earnedXP / neededXP * 100);
    }
    static float GetXp(ulong steamID, IBloodLegacy handler)
    {
        var xpData = handler.GetLegacyData(steamID);
        return xpData.Value;
    }
    public static int GetLevel(ulong steamID, IBloodLegacy handler)
    {
        var xpData = handler.GetLegacyData(steamID);
        return xpData.Key;
    }
    public static BloodType GetBloodTypeFromPrefab(PrefabGUID bloodPrefab)
    {
        string bloodCheck = bloodPrefab.GetPrefabName();

        return Enum.GetValues(typeof(BloodType))
            .Cast<BloodType>()
            .FirstOrDefault(type =>
            bloodCheck.Contains(type.ToString(), StringComparison.CurrentCultureIgnoreCase));
    }
}