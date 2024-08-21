using Bloodcraft.Patches;
using Bloodcraft.Services;
using Bloodcraft.SystemUtilities.Leveling;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
using static Bloodcraft.Utilities;

namespace Bloodcraft.SystemUtilities.Legacies;
public static class BloodSystem
{
    static EntityManager EntityManager => Core.EntityManager;
    static ConfigService ConfigService => Core.ConfigService;
    static LocalizationService LocalizationService => Core.LocalizationService;

    const float BloodConstant = 0.1f; // constant for calculating level from xp
    const int BloodPower = 2; // power for calculating level from xp
    public enum BloodType
    {
        Worker,
        Warrior,
        Scholar,
        Rogue,
        Mutant,
        VBlood,
        None,
        GateBoss,
        Draculin,
        Immortal,
        Creature,
        Brute
    }

    public static readonly Dictionary<BloodType, PrestigeSystem.PrestigeType> BloodPrestigeMap = new()
    {
        { BloodType.Worker, PrestigeSystem.PrestigeType.WorkerLegacy },
        { BloodType.Warrior, PrestigeSystem.PrestigeType.WarriorLegacy },
        { BloodType.Scholar, PrestigeSystem.PrestigeType.ScholarLegacy },
        { BloodType.Rogue, PrestigeSystem.PrestigeType.RogueLegacy },
        { BloodType.Mutant, PrestigeSystem.PrestigeType.MutantLegacy },
        { BloodType.Draculin, PrestigeSystem.PrestigeType.DraculinLegacy },
        { BloodType.Immortal, PrestigeSystem.PrestigeType.ImmortalLegacy },
        { BloodType.Creature, PrestigeSystem.PrestigeType.CreatureLegacy },
        { BloodType.Brute, PrestigeSystem.PrestigeType.BruteLegacy }
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
        BloodSystem.BloodType bloodType = BloodHandler.GetCurrentBloodType(Killer);
        if (bloodType.Equals(BloodType.None)) return;

        IBloodHandler handler = BloodHandlerFactory.GetBloodHandler(bloodType);
        if (handler != null)
        {
            // Check if the player leveled up
            var xpData = handler.GetLegacyData(steamID);

            if (xpData.Key >= ConfigService.MaxBloodLevel) return;

            float changeFactor = 1f;

            if (Core.DataStructures.PlayerPrestiges.TryGetValue(steamID, out var prestiges))
            {
                // Apply rate reduction with diminishing returns
                if (prestiges.TryGetValue(BloodPrestigeMap[bloodType], out var legacyPrestige) && legacyPrestige > 0)
                {
                    changeFactor -= (ConfigService.PrestigeRatesReducer * legacyPrestige);
                    changeFactor = MathF.Max(changeFactor, 0);
                }

                // Apply rate gain with linear increase
                if (prestiges.TryGetValue(PrestigeSystem.PrestigeType.Experience, out var xpPrestige) && xpPrestige > 0)
                {
                    changeFactor += 1 + (ConfigService.PrestigeRatesMultiplier * xpPrestige);
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
            handler.UpdateLegacyData(steamID, updatedXPData);
            handler.SaveChanges();
            NotifyPlayer(user, bloodType, BloodValue, leveledUp, newLevel, handler);
        }
    }
    public static void NotifyPlayer(User user, BloodSystem.BloodType bloodType, float gainedXP, bool leveledUp, int newLevel, IBloodHandler handler)
    {
        ulong steamID = user.PlatformId;
        gainedXP = (int)gainedXP; // Convert to integer if necessary
        int levelProgress = GetLevelProgress(steamID, handler); // Calculate the current progress to the next level

        if (leveledUp)
        {
            if (newLevel <= ConfigService.MaxBloodLevel) LocalizationService.HandleServerReply(EntityManager, user, $"<color=red>{bloodType}</color> legacy improved to [<color=white>{newLevel}</color>]");
            if (GetPlayerBool(steamID, "Reminders"))
            {
                if (Core.DataStructures.PlayerBloodStats.TryGetValue(steamID, out var bloodStats) && bloodStats.TryGetValue(bloodType, out var Stats))
                {
                    if (Stats.Count < ConfigService.LegacyStatChoices)
                    {
                        LocalizationService.HandleServerReply(EntityManager, user, $"{ConfigService.LegacyStatChoices - Stats.Count} stat bonuses available for <color=red>{bloodType.ToString().ToLower()}</color>; use '.bl cst {bloodType} [Stat]' to make your choice and '.bl lst' to view legacy stat options.");
                    }
                }
            }
        }
        
        if (Core.DataStructures.PlayerBools.TryGetValue(steamID, out var bools) && bools["BloodLogging"])
        {
            LocalizationService.HandleServerReply(EntityManager, user, $"+<color=yellow>{gainedXP}</color> <color=red>{bloodType}</color> <color=#FFC0CB>essence</color> (<color=white>{levelProgress}%</color>)");
        }
    }
    public static int GetLevelProgress(ulong SteamID, IBloodHandler handler)
    {
        float currentXP = GetXp(SteamID, handler);
        int currentLevelXP = ConvertLevelToXp(GetLevel(SteamID, handler));
        int nextLevelXP = ConvertLevelToXp(GetLevel(SteamID, handler) + 1);

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
    static int GetLevel(ulong steamID, IBloodHandler handler)
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