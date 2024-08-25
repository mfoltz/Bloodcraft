using Bloodcraft.Services;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
using Bloodcraft.Systems.Leveling;
using static Bloodcraft.Utilities;

namespace Bloodcraft.Systems.Expertise;
internal static class WeaponSystem
{
    static EntityManager EntityManager => Core.EntityManager;

    const float ExpertiseConstant = 0.1f; // constant for calculating level from xp
    const int ExpertisePower = 2; // power for calculating level from xp

    public static readonly Dictionary<WeaponType, Func<ulong, (bool Success, KeyValuePair<int, float> Data)>> TryGetExtensionMap = new()
    {
        { WeaponType.Sword, steamID =>
            {
                if (steamID.TryGetPlayerSwordExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { WeaponType.Axe, steamID =>
            {
                if (steamID.TryGetPlayerAxeExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { WeaponType.Mace, steamID =>
            {
                if (steamID.TryGetPlayerMaceExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { WeaponType.Spear, steamID =>
            {
                if (steamID.TryGetPlayerSpearExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { WeaponType.Crossbow, steamID =>
            {
                if (steamID.TryGetPlayerCrossbowExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { WeaponType.GreatSword, steamID =>
            {
                if (steamID.TryGetPlayerGreatSwordExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { WeaponType.Slashers, steamID =>
            {
                if (steamID.TryGetPlayerSlashersExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { WeaponType.Pistols, steamID =>
            {
                if (steamID.TryGetPlayerPistolsExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { WeaponType.Reaper, steamID =>
            {
                if (steamID.TryGetPlayerReaperExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { WeaponType.Longbow, steamID =>
            {
                if (steamID.TryGetPlayerLongbowExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { WeaponType.Whip, steamID =>
            {
                if (steamID.TryGetPlayerWhipExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { WeaponType.Unarmed, steamID =>
            {
                if (steamID.TryGetPlayerUnarmedExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        },
        { WeaponType.FishingPole, steamID =>
            {
                if (steamID.TryGetPlayerFishingPoleExpertise(out var data))
                {
                    return (true, data);
                }
                return (false, default);
            }
        }
    };

    public static readonly Dictionary<WeaponType, Action<ulong, KeyValuePair<int, float>>> SetExtensionMap = new()
    {
        { WeaponType.Sword, (steamID, data) => steamID.SetPlayerSwordExpertise(data) },
        { WeaponType.Axe, (steamID, data) => steamID.SetPlayerAxeExpertise(data) },
        { WeaponType.Mace, (steamID, data) => steamID.SetPlayerMaceExpertise(data) },
        { WeaponType.Spear, (steamID, data) => steamID.SetPlayerSpearExpertise(data) },
        { WeaponType.Crossbow, (steamID, data) => steamID.SetPlayerCrossbowExpertise(data) },
        { WeaponType.GreatSword, (steamID, data) => steamID.SetPlayerGreatSwordExpertise(data) },
        { WeaponType.Slashers, (steamID, data) => steamID.SetPlayerSlashersExpertise(data) },
        { WeaponType.Pistols, (steamID, data) => steamID.SetPlayerPistolsExpertise(data) },
        { WeaponType.Reaper, (steamID, data) => steamID.SetPlayerReaperExpertise(data) },
        { WeaponType.Longbow, (steamID, data) => steamID.SetPlayerLongbowExpertise(data) },
        { WeaponType.Whip, (steamID, data) => steamID.SetPlayerWhipExpertise(data) },
        { WeaponType.Unarmed, (steamID, data) => steamID.SetPlayerUnarmedExpertise(data) },
        { WeaponType.FishingPole, (steamID, data) => steamID.SetPlayerFishingPoleExpertise(data) }
    };

    public static readonly Dictionary<WeaponType, PrestigeType> WeaponPrestigeMap = new()
    {
        { WeaponType.Sword, PrestigeType.SwordExpertise },
        { WeaponType.Axe, PrestigeType.AxeExpertise },
        { WeaponType.Mace, PrestigeType.MaceExpertise },
        { WeaponType.Spear, PrestigeType.SpearExpertise },
        { WeaponType.Crossbow, PrestigeType.CrossbowExpertise },
        { WeaponType.GreatSword, PrestigeType.GreatSwordExpertise },
        { WeaponType.Slashers, PrestigeType.SlashersExpertise },
        { WeaponType.Pistols, PrestigeType.PistolsExpertise },
        { WeaponType.Reaper, PrestigeType.ReaperExpertise },
        { WeaponType.Longbow, PrestigeType.LongbowExpertise },
        { WeaponType.Whip, PrestigeType.WhipExpertise },
        { WeaponType.Unarmed, PrestigeType.UnarmedExpertise }, 
        { WeaponType.FishingPole, PrestigeType.FishingPoleExpertise }
    };
    public static void UpdateExpertise(Entity Killer, Entity Victim)
    {
        if (Killer == Victim || Victim.Has<Minion>()) return;

        Entity userEntity = Killer.Read<PlayerCharacter>().UserEntity;
        User user = userEntity.Read<User>();
        ulong steamID = user.PlatformId;
        WeaponType weaponType = WeaponManager.GetCurrentWeaponType(Killer);

        if (Victim.Has<UnitStats>())
        {
            var VictimStats = Victim.Read<UnitStats>();
            float ExpertiseValue = CalculateExpertiseValue(VictimStats, Victim.Has<VBloodConsumeSource>());
            float changeFactor = 1f;

            if (steamID.TryGetPlayerPrestiges(out var prestiges))
            {
                if (prestiges.TryGetValue(WeaponPrestigeMap[weaponType], out var expertisePrestige))
                {
                    changeFactor -= (ConfigService.PrestigeRatesReducer * expertisePrestige);
                }

                if (prestiges.TryGetValue(PrestigeType.Experience, out var xpPrestige))
                {
                    changeFactor += (ConfigService.PrestigeRateMultiplier * xpPrestige);
                }
            }

            ExpertiseValue *= changeFactor;
            IExpertiseHandler handler = ExpertiseHandlerFactory.GetExpertiseHandler(weaponType);
            if (handler != null)
            {
                // Check if the player leveled up
                var xpData = handler.GetExpertiseData(steamID);

                if (xpData.Key >= ConfigService.MaxExpertiseLevel) return;

                float newExperience = xpData.Value + ExpertiseValue;
                int newLevel = ConvertXpToLevel(newExperience);
                bool leveledUp = false;

                if (newLevel > xpData.Key)
                {
                    leveledUp = true;
                    if (newLevel > ConfigService.MaxExpertiseLevel)
                    {
                        newLevel = ConfigService.MaxExpertiseLevel;
                        newExperience = ConvertLevelToXp(ConfigService.MaxExpertiseLevel);
                    }
                }

                var updatedXPData = new KeyValuePair<int, float>(newLevel, newExperience);
                handler.SetExpertiseData(steamID, updatedXPData);
                NotifyPlayer(user, weaponType, ExpertiseValue, leveledUp, newLevel, handler);
            }
        }
    }
    static float CalculateExpertiseValue(UnitStats VictimStats, bool isVBlood)
    {
        float ExpertiseValue = VictimStats.SpellPower + VictimStats.PhysicalPower;
        if (isVBlood) return ExpertiseValue * ConfigService.VBloodExpertiseMultiplier;
        return ExpertiseValue * ConfigService.UnitExpertiseMultiplier;
    }
    static void NotifyPlayer(User user, WeaponType weaponType, float gainedXP, bool leveledUp, int newLevel, IExpertiseHandler handler)
    {
        ulong steamID = user.PlatformId;
        gainedXP = (int)gainedXP;
        int levelProgress = GetLevelProgress(steamID, handler);

        if (leveledUp)
        {
            if (newLevel <= ConfigService.MaxExpertiseLevel) LocalizationService.HandleServerReply(EntityManager, user, $"<color=#c0c0c0>{weaponType}</color> improved to [<color=white>{newLevel}</color>]");
            if (GetPlayerBool(steamID, "Reminders"))
            {
                if (steamID.TryGetPlayerWeaponStats(out var weaponStats) && weaponStats.TryGetValue(weaponType, out var Stats))
                {
                    if (Stats.Count < ConfigService.ExpertiseStatChoices)
                    {
                        LocalizationService.HandleServerReply(EntityManager, user, $"{ConfigService.ExpertiseStatChoices - Stats.Count} stat bonuses available for <color=#c0c0c0>{weaponType.ToString().ToLower()}</color>; use '.wep cst {weaponType} [Stat]' to make your choice and '.wep lst' to view expertise stat options.");
                    }
                }
            }
        }

        if (GetPlayerBool(steamID, "ExpertiseLogging"))
        {
            LocalizationService.HandleServerReply(EntityManager, user, $"+<color=yellow>{gainedXP}</color> <color=#c0c0c0>{weaponType.ToString().ToLower()}</color> <color=#FFC0CB>expertise</color> (<color=white>{levelProgress}%</color>)");
        }
    }
    public static int GetLevelProgress(ulong steamID, IExpertiseHandler handler)
    {
        float currentXP = GetXp(steamID, handler);
        int currentLevelXP = ConvertLevelToXp(GetLevelFromXp(steamID, handler));
        int nextLevelXP = ConvertLevelToXp(GetLevelFromXp(steamID, handler) + 1);

        double neededXP = nextLevelXP - currentLevelXP;
        double earnedXP = nextLevelXP - currentXP;
        return 100 - (int)Math.Ceiling(earnedXP / neededXP * 100);
    }
    static int ConvertXpToLevel(float xp)
    {
        return (int)(ExpertiseConstant * Math.Sqrt(xp));
    }
    public static int ConvertLevelToXp(int level)
    {
        return (int)Math.Pow(level / ExpertiseConstant, ExpertisePower);
    }
    static float GetXp(ulong steamID, IExpertiseHandler handler)
    {
        var xpData = handler.GetExpertiseData(steamID);
        return xpData.Value;
    }
    public static int GetLevel(ulong steamID, IExpertiseHandler handler)
    {
        var xpData = handler.GetExpertiseData(steamID);
        return xpData.Key;
    }
    static int GetLevelFromXp(ulong steamID, IExpertiseHandler handler)
    {
        return ConvertXpToLevel(GetXp(steamID, handler));
    }
    public static WeaponType GetWeaponTypeFromSlotEntity(Entity weaponEntity)
    {
        if (weaponEntity == Entity.Null) return WeaponType.Unarmed;
        string weaponCheck = weaponEntity.Read<PrefabGUID>().LookupName();

        return Enum.GetValues(typeof(WeaponType))
            .Cast<WeaponType>()
            .FirstOrDefault(type =>
            weaponCheck.Contains(type.ToString(), StringComparison.OrdinalIgnoreCase) &&
            !(type == WeaponType.Sword && weaponCheck.Contains("GreatSword", StringComparison.OrdinalIgnoreCase))
            );
    }
}