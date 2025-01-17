using Bloodcraft.Services;
using Bloodcraft.Systems.Leveling;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
using UnityEngine;
using static Bloodcraft.Utilities.Misc.PlayerBoolsManager;
using static Bloodcraft.Utilities.Progression;

namespace Bloodcraft.Systems.Legacies;
internal static class BloodSystem
{
    static EntityManager EntityManager => Core.EntityManager;

    static readonly int _maxBloodLevel = ConfigService.MaxBloodLevel;
    static readonly int _legacyStatChoices = ConfigService.LegacyStatChoices;
    static readonly float _vBloodLegacyMultiplier = ConfigService.VBloodLegacyMultiplier;
    static readonly float _unitLegacyMultiplier = ConfigService.UnitLegacyMultiplier;

    const int BASE_BLOOD_FACTOR = 10;
    const float BLOOD_TYPE_FACTOR = 3f;

    public static readonly Dictionary<BloodType, Func<ulong, (bool Success, KeyValuePair<int, float> Data)>> TryGetExtensionMap = new()
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
        { BloodType.VBlood, steamID =>
            {
                if (steamID.TryGetPlayerVBloodLegacy(out var data))
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
        }
    };

    public static readonly Dictionary<BloodType, Action<ulong, KeyValuePair<int, float>>> SetExtensionMap = new()
    {
        { BloodType.Worker, (steamID, data) => steamID.SetPlayerWorkerLegacy(data) },
        { BloodType.Warrior, (steamID, data) => steamID.SetPlayerWarriorLegacy(data) },
        { BloodType.Scholar, (steamID, data) => steamID.SetPlayerScholarLegacy(data) },
        { BloodType.Rogue, (steamID, data) => steamID.SetPlayerRogueLegacy(data) },
        { BloodType.Mutant, (steamID, data) => steamID.SetPlayerMutantLegacy(data) },
        { BloodType.VBlood, (steamID, data) => steamID.SetPlayerVBloodLegacy(data) },
        { BloodType.Draculin, (steamID, data) => steamID.SetPlayerDraculinLegacy(data) },
        { BloodType.Immortal, (steamID, data) => steamID.SetPlayerImmortalLegacy(data) },
        { BloodType.Creature, (steamID, data) => steamID.SetPlayerCreatureLegacy(data) },
        { BloodType.Brute, (steamID, data) => steamID.SetPlayerBruteLegacy(data) }
    };

    public static readonly Dictionary<BloodType, PrestigeType> BloodTypeToPrestigeMap = new()
    {
        { BloodType.Worker, PrestigeType.WorkerLegacy },
        { BloodType.Warrior, PrestigeType.WarriorLegacy },
        { BloodType.Scholar, PrestigeType.ScholarLegacy },
        { BloodType.Rogue, PrestigeType.RogueLegacy },
        { BloodType.Mutant, PrestigeType.MutantLegacy },
        { BloodType.Draculin, PrestigeType.DraculinLegacy },
        { BloodType.Immortal, PrestigeType.ImmortalLegacy },
        { BloodType.Creature, PrestigeType.CreatureLegacy },
        { BloodType.Brute, PrestigeType.BruteLegacy }
    };

    public static readonly Dictionary<PrefabGUID, BloodType> BuffToBloodTypeMap = new() // base buffs present regardless of blood quality indicating type consumed
    {
        { new PrefabGUID(-773025435), BloodType.Worker }, // yield bonus
        { new PrefabGUID(-804597757), BloodType.Warrior }, // phys bonus
        { new PrefabGUID(1934870645), BloodType.Scholar }, // spell bonus
        { new PrefabGUID(1201299233), BloodType.Rogue }, // crit bonus
        { new PrefabGUID(-1266262267), BloodType.Mutant }, // drain bonus
        { new PrefabGUID(560247144), BloodType.VBlood }, // vblood_0
        { new PrefabGUID(1558171501), BloodType.Draculin }, // speed bonus
        { new PrefabGUID(-488475343), BloodType.Immortal }, // phys & spell bonus
        { new PrefabGUID(894725875), BloodType.Creature }, // speed bonus
        { new PrefabGUID(1828387635), BloodType.Brute } // primary life leech
    };

    public static readonly Dictionary<BloodType, PrefabGUID> BloodTypeToBuffMap = new()
    {
        { BloodType.Worker, new PrefabGUID(-773025435) }, // yield bonus
        { BloodType.Warrior, new PrefabGUID(-804597757) }, // phys bonus
        { BloodType.Scholar, new PrefabGUID(1934870645) }, // spell bonus
        { BloodType.Rogue, new PrefabGUID(1201299233) }, // crit bonus
        { BloodType.Mutant, new PrefabGUID(-1266262267) }, // drain bonus
        { BloodType.VBlood, new PrefabGUID(560247144) }, // vblood_0
        { BloodType.Draculin, new PrefabGUID(1558171501) }, // speed bonus
        { BloodType.Immortal, new PrefabGUID(-488475343) }, // phys/spell bonus
        { BloodType.Creature, new PrefabGUID(894725875) }, // speed bonus
        { BloodType.Brute, new PrefabGUID(1828387635) } // primary life leech
    };

    public static readonly Dictionary<BloodType, PrefabGUID> BloodTypeToConsumeSourceMap = new()
    {
        { BloodType.Worker, new PrefabGUID(1743532914) }, // CHAR_Bandit_Worker_Gatherer
        { BloodType.Warrior, new PrefabGUID(923140362) }, // CHAR_Bandit_Thief
        { BloodType.Scholar, new PrefabGUID(-700632469) }, // CHAR_Militia_Nun
        { BloodType.Rogue, new PrefabGUID(1220569089) }, // CHAR_Bandit_Scout
        { BloodType.Mutant, new PrefabGUID(1092792896) }, // CHAR_Mutant_Spitter
        { BloodType.Draculin, new PrefabGUID(-494298686) }, // CHAR_Legion_NightMaiden
        { BloodType.Creature, new PrefabGUID(-218175217) }, // CHAR_Cursed_Wolf
        { BloodType.Immortal, new PrefabGUID(55100532) }, // CHAR_Dracula_BloodSoul_heart
        { BloodType.Brute, new PrefabGUID(2005508157) } // CHAR_Militia_Heavy
    };
    public static void ProcessLegacy(Entity source, Entity target)
    {
        if (!target.Has<BloodConsumeSource>()) return;

        BloodConsumeSource bloodConsumeSource = target.Read<BloodConsumeSource>();
        BloodType targetBloodType = GetBloodTypeFromPrefab(bloodConsumeSource.UnitBloodType._Value);

        int unitLevel = target.Read<UnitLevel>().Level;
        float bloodValue = 0;

        if (target.Has<VBloodConsumeSource>())
        {
            bloodValue = BASE_BLOOD_FACTOR * unitLevel * _vBloodLegacyMultiplier;
        }
        else
        {
            bloodValue = bloodConsumeSource.BloodQuality / BASE_BLOOD_FACTOR * unitLevel * _unitLegacyMultiplier;
        }

        Entity userEntity = source.Read<PlayerCharacter>().UserEntity;
        User user = userEntity.Read<User>();
        ulong steamID = user.PlatformId;

        BloodType bloodType = BloodManager.GetCurrentBloodType(source);
        float bloodQuality = source.Read<Blood>().Quality;

        if (bloodType.Equals(BloodType.None)) return;
        else if (targetBloodType.Equals(bloodType))
        {
            bloodValue *= BLOOD_TYPE_FACTOR; // same type multiplier
        }

        float qualityMultiplier = 1f + (bloodQuality / 100f);
        qualityMultiplier = Mathf.Min(qualityMultiplier, 2f); // Cap the multiplier at 2
        bloodValue *= qualityMultiplier;

        IBloodHandler handler = BloodHandlerFactory.GetBloodHandler(bloodType);
        if (handler != null)
        {
            SaveBloodExperience(steamID, handler, bloodValue, out bool leveledUp, out int newLevel);
            NotifyPlayer(user, bloodType, bloodValue, leveledUp, newLevel, handler);
        }
    }
    public static void SaveBloodExperience(ulong steamID, IBloodHandler handler, float gainedXP, out bool leveledUp, out int newLevel)
    {
        var xpData = handler.GetLegacyData(steamID);
        int currentLevel = xpData.Key;
        float currentXP = xpData.Value;

        if (currentLevel >= _maxBloodLevel)
        {
            // Already at max level, no changes
            leveledUp = false;
            newLevel = currentLevel;
            return;
        }

        float newExperience = currentXP + gainedXP;
        newLevel = ConvertXpToLevel(newExperience);
        leveledUp = false;

        // Check level-up and cap at max
        if (newLevel > currentLevel)
        {
            leveledUp = true;
            if (newLevel > _maxBloodLevel)
            {
                newLevel = _maxBloodLevel;
                newExperience = ConvertLevelToXp(_maxBloodLevel);
            }
        }

        handler.SetLegacyData(steamID, new KeyValuePair<int, float>(newLevel, newExperience));
    }
    static void HandleBloodLevelUp(User user, BloodType bloodType, int newLevel, ulong steamID)
    {
        if (newLevel <= _maxBloodLevel)
        {
            LocalizationService.HandleServerReply(EntityManager, user,
                $"<color=red>{bloodType}</color> legacy improved to [<color=white>{newLevel}</color>]");
        }

        if (GetPlayerBool(steamID, "Reminders"))
        {
            if (steamID.TryGetPlayerBloodStats(out var bloodStats) && bloodStats.TryGetValue(bloodType, out var stats))
            {
                int currentStatCount = stats.Count;
                if (currentStatCount < _legacyStatChoices)
                {
                    int choicesLeft = _legacyStatChoices - currentStatCount;
                    string bonusString = choicesLeft > 1 ? "bonuses" : "bonus";
                    LocalizationService.HandleServerReply(EntityManager, user,
                        $"{choicesLeft} <color=white>stat</color> <color=#00FFFF>{bonusString}</color> available for <color=red>{bloodType.ToString().ToLower()}</color>; use '<color=white>.bl cst {bloodType} [Stat]</color>' to choose and '<color=white>.bl lst</color>' to view legacy stat options. (toggle reminders with <color=white>'.remindme'</color>)");
                }
            }
        }
    }
    public static void NotifyPlayer(User user, BloodType bloodType, float gainedXP, bool leveledUp, int newLevel, IBloodHandler handler)
    {
        ulong steamID = user.PlatformId;

        int gainedIntXP = (int)gainedXP;
        int levelProgress = GetLevelProgress(steamID, handler);

        if (leveledUp)
        {
            HandleBloodLevelUp(user, bloodType, newLevel, steamID);
        }
        else if (newLevel >= _maxBloodLevel) return;
        else if (GetPlayerBool(steamID, "BloodLogging"))
        {
            LocalizationService.HandleServerReply(EntityManager, user,
                $"+<color=yellow>{gainedIntXP}</color> <color=red>{bloodType}</color> <color=#FFC0CB>essence</color> (<color=white>{levelProgress}%</color>)");
        }
    }
    public static int GetLevelProgress(ulong SteamID, IBloodHandler handler)
    {
        int currentLevel = GetLevel(SteamID, handler);
        float currentXP = GetXp(SteamID, handler);

        int currentLevelXP = ConvertLevelToXp(currentLevel);
        int nextLevelXP = ConvertLevelToXp(++currentLevel);

        double neededXP = nextLevelXP - currentLevelXP;
        double earnedXP = nextLevelXP - currentXP;

        return 100 - (int)Math.Ceiling(earnedXP / neededXP * 100);
    }
    static float GetXp(ulong steamID, IBloodHandler handler)
    {
        var xpData = handler.GetLegacyData(steamID);
        return xpData.Value;
    }
    public static int GetLevel(ulong steamID, IBloodHandler handler)
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
            bloodCheck.Contains(type.ToString(), StringComparison.OrdinalIgnoreCase));
    }
}