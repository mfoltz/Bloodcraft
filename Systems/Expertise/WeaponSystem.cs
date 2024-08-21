using Bloodcraft.Patches;
using Bloodcraft.Services;
using Bloodcraft.SystemUtilities.Leveling;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
using static Bloodcraft.Utilities;

namespace Bloodcraft.SystemUtilities.Expertise;
public static class WeaponSystem
{
    static EntityManager EntityManager => Core.EntityManager;
    static ConfigService ConfigService => Core.ConfigService;
    static LocalizationService LocalizationService => Core.LocalizationService;

    const float ExpertiseConstant = 0.1f; // constant for calculating level from xp
    const int ExpertisePower = 2; // power for calculating level from xp
    public enum WeaponType
    {
        Sword,
        Axe,
        Mace,
        Spear,
        Crossbow,
        GreatSword,
        Slashers,
        Pistols,
        Reaper,
        Longbow,
        Whip,
        Unarmed,
        FishingPole
    }

    public static readonly Dictionary<WeaponType, PrestigeSystem.PrestigeType> WeaponPrestigeMap = new()
    {
        { WeaponType.Sword, PrestigeSystem.PrestigeType.SwordExpertise },
        { WeaponType.Axe, PrestigeSystem.PrestigeType.AxeExpertise },
        { WeaponType.Mace, PrestigeSystem.PrestigeType.MaceExpertise },
        { WeaponType.Spear, PrestigeSystem.PrestigeType.SpearExpertise },
        { WeaponType.Crossbow, PrestigeSystem.PrestigeType.CrossbowExpertise },
        { WeaponType.GreatSword, PrestigeSystem.PrestigeType.GreatSwordExpertise },
        { WeaponType.Slashers, PrestigeSystem.PrestigeType.SlashersExpertise },
        { WeaponType.Pistols, PrestigeSystem.PrestigeType.PistolsExpertise },
        { WeaponType.Reaper, PrestigeSystem.PrestigeType.ReaperExpertise },
        { WeaponType.Longbow, PrestigeSystem.PrestigeType.LongbowExpertise },
        { WeaponType.Whip, PrestigeSystem.PrestigeType.WhipExpertise },
        { WeaponType.Unarmed, PrestigeSystem.PrestigeType.UnarmedExpertise }, 
        { WeaponType.FishingPole, PrestigeSystem.PrestigeType.FishingPoleExpertise }
    };
    public static void UpdateExpertise(Entity Killer, Entity Victim)
    {
        if (Killer == Victim || Victim.Has<Minion>()) return;

        Entity userEntity = Killer.Read<PlayerCharacter>().UserEntity;
        User user = userEntity.Read<User>();
        ulong steamID = user.PlatformId;
        WeaponType weaponType = WeaponHandler.GetCurrentWeaponType(Killer);

        if (Victim.Has<UnitStats>())
        {
            var VictimStats = Victim.Read<UnitStats>();
            float ExpertiseValue = CalculateExpertiseValue(VictimStats, Victim.Has<VBloodConsumeSource>());
            float changeFactor = 1f;

            if (Core.DataStructures.PlayerPrestiges.TryGetValue(steamID, out var prestiges))
            {
                if (prestiges.TryGetValue(WeaponPrestigeMap[weaponType], out var expertisePrestige) && expertisePrestige > 0)
                {
                    changeFactor -= (ConfigService.PrestigeRatesReducer * expertisePrestige);
                }

                if (prestiges.TryGetValue(PrestigeSystem.PrestigeType.Experience, out var xpPrestige) && xpPrestige > 0)
                {
                    changeFactor += (ConfigService.PrestigeRatesMultiplier * xpPrestige);
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
                    // update stats here?

                }

                var updatedXPData = new KeyValuePair<int, float>(newLevel, newExperience);
                handler.UpdateExpertiseData(steamID, updatedXPData);
                handler.SaveChanges();
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
                if (Core.DataStructures.PlayerWeaponStats.TryGetValue(steamID, out var weaponStats) && weaponStats.TryGetValue(weaponType, out var Stats))
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
        int currentLevelXP = ConvertLevelToXp(GetLevel(steamID, handler));
        int nextLevelXP = ConvertLevelToXp(GetLevel(steamID, handler) + 1);

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
    static int GetLevel(ulong steamID, IExpertiseHandler handler)
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