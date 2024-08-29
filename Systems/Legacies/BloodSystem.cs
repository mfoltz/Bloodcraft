﻿using Bloodcraft.Services;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
using Bloodcraft.Systems.Leveling;
using static Bloodcraft.Utilities;

namespace Bloodcraft.Systems.Legacies;
internal static class BloodSystem
{
    static EntityManager EntityManager => Core.EntityManager;

    const float BloodConstant = 0.1f;
    const int BloodPower = 2;

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

    public static readonly Dictionary<BloodType, PrestigeType> BloodPrestigeMap = new()
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

    public static readonly Dictionary<PrefabGUID, BloodType> BuffToBloodTypeMap = new()
{
        { new PrefabGUID(-773025435), BloodType.Worker }, // yield bonus
        { new PrefabGUID(-804597757), BloodType.Warrior }, // phys bonus
        { new PrefabGUID(1934870645), BloodType.Scholar }, // spell bonus
        { new PrefabGUID(1201299233), BloodType.Rogue }, // crit bonus
        { new PrefabGUID(-1266262267), BloodType.Mutant }, // drain bonus
        { new PrefabGUID(560247144), BloodType.VBlood }, // vblood_0
        { new PrefabGUID(1558171501), BloodType.Draculin }, // speed bonus
        { new PrefabGUID(-488475343), BloodType.Immortal }, // phys/spell bonus
        { new PrefabGUID(894725875), BloodType.Creature }, // speed bonus
        { new PrefabGUID(1828387635), BloodType.Brute } // primary life leech
    };
    public static void UpdateLegacy(Entity Killer, Entity Victim)
    {
        if (Killer == Victim || Victim.Has<Minion>() || !Victim.Has<BloodConsumeSource>() || !Victim.Has<UnitLevel>()) return;
        BloodConsumeSource bloodConsumeSource = Victim.Read<BloodConsumeSource>();

        Entity userEntity = Killer.Read<PlayerCharacter>().UserEntity;
        int unitLevel = Victim.Read<UnitLevel>().Level;
        float BloodValue = 0;
        if (Victim.Has<VBloodConsumeSource>())
        {
            BloodValue = 10 * unitLevel * ConfigService.VBloodLegacyMultiplier;
        }
        else
        {
            BloodValue = bloodConsumeSource.BloodQuality / 10 * unitLevel * ConfigService.UnitLegacyMultiplier;
        }

        User user = userEntity.Read<User>();
        ulong steamID = user.PlatformId;
        BloodType bloodType = BloodManager.GetCurrentBloodType(Killer);
        if (bloodType.Equals(BloodType.None)) return;

        IBloodHandler handler = BloodHandlerFactory.GetBloodHandler(bloodType);
        if (handler != null)
        {
            // Check if the player leveled up
            var xpData = handler.GetLegacyData(steamID);

            if (xpData.Key >= ConfigService.MaxBloodLevel) return;

            float changeFactor = 1f;

            if (steamID.TryGetPlayerPrestiges(out var prestiges))
            {
                // Apply rate reduction with diminishing returns
                if (prestiges.TryGetValue(BloodPrestigeMap[bloodType], out var legacyPrestige))
                {
                    changeFactor -= (ConfigService.PrestigeRatesReducer * legacyPrestige);
                    changeFactor = MathF.Max(changeFactor, 0);
                }

                // Apply rate gain with linear increase
                if (prestiges.TryGetValue(PrestigeType.Experience, out var xpPrestige))
                {
                    changeFactor += 1 + (ConfigService.PrestigeRateMultiplier * xpPrestige);
                }
            }

            BloodValue *= changeFactor;

            float newExperience = xpData.Value + BloodValue;
            int newLevel = ConvertXpToLevel(newExperience);
            bool leveledUp = false;

            if (newLevel > xpData.Key)
            {
                leveledUp = true;
                if (newLevel > ConfigService.MaxBloodLevel)
                {
                    newLevel = ConfigService.MaxBloodLevel;
                    newExperience = ConvertLevelToXp(ConfigService.MaxBloodLevel);
                }
            }

            var updatedXPData = new KeyValuePair<int, float>(newLevel, newExperience);
            handler.SetLegacyData(steamID, updatedXPData);
            NotifyPlayer(user, bloodType, BloodValue, leveledUp, newLevel, handler);
        }
    }
    public static void NotifyPlayer(User user, BloodType bloodType, float gainedXP, bool leveledUp, int newLevel, IBloodHandler handler)
    {
        ulong steamID = user.PlatformId;
        gainedXP = (int)gainedXP; // Convert to integer if necessary
        int levelProgress = GetLevelProgress(steamID, handler); // Calculate the current progress to the next level

        if (leveledUp)
        {
            if (newLevel <= ConfigService.MaxBloodLevel) LocalizationService.HandleServerReply(EntityManager, user, $"<color=red>{bloodType}</color> legacy improved to [<color=white>{newLevel}</color>]");
            if (GetPlayerBool(steamID, "Reminders"))
            {
                if (steamID.TryGetPlayerBloodStats(out var bloodStats) && bloodStats.TryGetValue(bloodType, out var Stats))
                {
                    if (Stats.Count < ConfigService.LegacyStatChoices)
                    {
                        LocalizationService.HandleServerReply(EntityManager, user, $"{ConfigService.LegacyStatChoices - Stats.Count} <color=white>stat</color> <color=#00FFFF>bonuses</color> available for <color=red>{bloodType.ToString().ToLower()}</color>; use '<color=white>.bl cst {bloodType} [Stat]</color>' to make your choice and '<color=white>.bl lst</color>' to view legacy stat options.");
                    }
                }
            }
        }
        
        if (GetPlayerBool(steamID, "BloodLogging"))
        {
            LocalizationService.HandleServerReply(EntityManager, user, $"+<color=yellow>{gainedXP}</color> <color=red>{bloodType}</color> <color=#FFC0CB>essence</color> (<color=white>{levelProgress}%</color>)");
        }
    }
    public static int GetLevelProgress(ulong SteamID, IBloodHandler handler)
    {
        float currentXP = GetXp(SteamID, handler);
        int currentLevelXP = ConvertLevelToXp(GetLevelFromXp(SteamID, handler));
        int nextLevelXP = ConvertLevelToXp(GetLevelFromXp(SteamID, handler) + 1);

        double neededXP = nextLevelXP - currentLevelXP;
        double earnedXP = nextLevelXP - currentXP;

        return 100 - (int)Math.Ceiling(earnedXP / neededXP * 100);
    }
    public static int ConvertXpToLevel(float xp)
    {
        // Assuming a basic square root scaling for experience to level conversion
        return (int)(BloodConstant * Math.Sqrt(xp));
    }
    public static int ConvertLevelToXp(int level)
    {
        // Reversing the formula used in ConvertXpToLevel for consistency
        return (int)Math.Pow(level / BloodConstant, BloodPower);
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
    static int GetLevelFromXp(ulong steamID, IBloodHandler handler)
    {
        return ConvertXpToLevel(GetXp(steamID, handler));
    }
    public static BloodType GetBloodTypeFromPrefab(PrefabGUID bloodPrefab)
    {
        string bloodCheck = bloodPrefab.LookupName();
        return Enum.GetValues(typeof(BloodType))
            .Cast<BloodType>()
            .FirstOrDefault(type =>
            bloodCheck.Contains(type.ToString(), StringComparison.OrdinalIgnoreCase));
    }
}