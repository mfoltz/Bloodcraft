using Bloodcraft.Services;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Utilities;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;
using static Bloodcraft.Patches.DeathEventListenerSystemPatch;
using User = ProjectM.Network.User;

namespace Bloodcraft.Systems.Leveling;
internal static class LevelingSystem
{
    static EntityManager EntityManager => Core.EntityManager;

    const float EXP_CONSTANT = 0.1f; // constant for calculating level from xp
    const float EXP_POWER = 2f; // power for calculating level from xp

    static readonly bool Classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;
    static readonly bool Familiars = ConfigService.FamiliarSystem;
    static readonly bool RestedXPSystem = ConfigService.RestedXPSystem;

    static readonly int MaxPlayerLevel = ConfigService.MaxLevel;
    static readonly float GroupMultiplier = ConfigService.GroupLevelingMultiplier;
    static readonly int RestedXPMax = ConfigService.RestedXPMax;

    static readonly float UnitSpawnerMultiplier = ConfigService.UnitSpawnerMultiplier;
    static readonly float WarEventMultiplier = ConfigService.WarEventMultiplier;
    static readonly float DocileUnitMultiplier = ConfigService.DocileUnitMultiplier;
    static readonly float LevelScalingMultiplier = ConfigService.LevelScalingMultiplier;

    static readonly float VBloodLevelingMultiplier = ConfigService.VBloodLevelingMultiplier;
    static readonly float UnitLevelingMultiplier = ConfigService.UnitLevelingMultiplier;

    static readonly float LevelingPrestigeReducer = ConfigService.LevelingPrestigeReducer;

    static readonly PrefabGUID LevelUpBuff = new(-1133938228);
    static readonly PrefabGUID WarEventTrash = new(2090187901);
    public enum PlayerClass
    {
        BloodKnight,
        DemonHunter,
        VampireLord,
        ShadowBlade,
        ArcaneSorcerer,
        DeathMage
    }

    public static readonly Dictionary<PlayerClass, (string, string)> ClassWeaponBloodMap = new()
    {
        { PlayerClass.BloodKnight, (ConfigService.BloodKnightWeapon, ConfigService.BloodKnightBlood) },
        { PlayerClass.DemonHunter, (ConfigService.DemonHunterWeapon, ConfigService.DemonHunterBlood) },
        { PlayerClass.VampireLord, (ConfigService.VampireLordWeapon, ConfigService.VampireLordBlood) },
        { PlayerClass.ShadowBlade, (ConfigService.ShadowBladeWeapon, ConfigService.ShadowBladeBlood) },
        { PlayerClass.ArcaneSorcerer, (ConfigService.ArcaneSorcererWeapon, ConfigService.ArcaneSorcererBlood) },
        { PlayerClass.DeathMage, (ConfigService.DeathMageWeapon, ConfigService.DeathMageBlood) }
    };

    public static readonly Dictionary<PlayerClass, (List<int>, List<int>)> ClassWeaponBloodEnumMap = new()
    {
        { PlayerClass.BloodKnight, (ConfigUtilities.ParseConfigIntegerString(ConfigService.BloodKnightWeapon), ConfigUtilities.ParseConfigIntegerString(ConfigService.BloodKnightBlood)) },
        { PlayerClass.DemonHunter, (ConfigUtilities.ParseConfigIntegerString(ConfigService.DemonHunterWeapon), ConfigUtilities.ParseConfigIntegerString(ConfigService.DemonHunterBlood)) },
        { PlayerClass.VampireLord, (ConfigUtilities.ParseConfigIntegerString(ConfigService.VampireLordWeapon), ConfigUtilities.ParseConfigIntegerString(ConfigService.VampireLordBlood)) },
        { PlayerClass.ShadowBlade, (ConfigUtilities.ParseConfigIntegerString(ConfigService.ShadowBladeWeapon), ConfigUtilities.ParseConfigIntegerString(ConfigService.ShadowBladeBlood)) },
        { PlayerClass.ArcaneSorcerer, (ConfigUtilities.ParseConfigIntegerString(ConfigService.ArcaneSorcererWeapon), ConfigUtilities.ParseConfigIntegerString(ConfigService.ArcaneSorcererBlood)) },
        { PlayerClass.DeathMage, (ConfigUtilities.ParseConfigIntegerString(ConfigService.DeathMageWeapon), ConfigUtilities.ParseConfigIntegerString(ConfigService.DeathMageBlood)) }
    };

    public static readonly Dictionary<PlayerClass, string> ClassBuffMap = new()
    {
        { PlayerClass.BloodKnight, ConfigService.BloodKnightBuffs },
        { PlayerClass.DemonHunter, ConfigService.DemonHunterBuffs },
        { PlayerClass.VampireLord, ConfigService.VampireLordBuffs },
        { PlayerClass.ShadowBlade, ConfigService.ShadowBladeBuffs },
        { PlayerClass.ArcaneSorcerer, ConfigService.ArcaneSorcererBuffs },
        { PlayerClass.DeathMage, ConfigService.DeathMageBuffs }
    };

    public static readonly Dictionary<PlayerClass, string> ClassSpellsMap = new()
    {
        { PlayerClass.BloodKnight, ConfigService.BloodKnightSpells },
        { PlayerClass.DemonHunter, ConfigService.DemonHunterSpells },
        { PlayerClass.VampireLord, ConfigService.VampireLordSpells },
        { PlayerClass.ShadowBlade, ConfigService.ShadowBladeSpells },
        { PlayerClass.ArcaneSorcerer, ConfigService.ArcaneSorcererSpells },
        { PlayerClass.DeathMage, ConfigService.DeathMageSpells }
    };
    public static void OnUpdate(object sender, DeathEventArgs deathEvent)
    {
        ProcessExperience(deathEvent.Source, deathEvent.Target, deathEvent.DeathParticipants);
    }
    public static void ProcessExperience(Entity source, Entity target, HashSet<Entity> deathParticipants)
    {     
        float groupMultiplier = 1f;
        bool inGroup = deathParticipants.Count > 1;

        if (inGroup) groupMultiplier = GroupMultiplier; // if more than 1 participant, apply group multiplier

        foreach (Entity player in deathParticipants)
        {
            ulong steamId = player.GetSteamId();

            if (Familiars) FamiliarLevelingSystem.ProcessFamiliarExperience(player, target, steamId, groupMultiplier);

            int currentLevel = steamId.TryGetPlayerExperience(out var xpData) ? xpData.Key : 0;
            bool maxLevel = currentLevel >= MaxPlayerLevel;

            if (maxLevel && inGroup) // if at max level, prestige or no, and in a group (party or clan) get expertise exp instead
            {
                WeaponSystem.ProcessExpertise(player, target, groupMultiplier);
            }
            else if (maxLevel) return;
            else ProcessExperienceGain(player, target, steamId, currentLevel, groupMultiplier);
        }
    }
    public static void ProcessExperienceGain(Entity source, Entity target, ulong steamId, int currentLevel, float groupMultiplier = 1f)
    {
        UnitLevel victimLevel = target.Read<UnitLevel>();
        Health health = target.Read<Health>();

        bool isVBlood = IsVBlood(target);

        int additionalXP = (int)(health.MaxHealth._Value / 2.5f);
        float gainedXP = GetBaseExperience(victimLevel.Level._Value, isVBlood);

        gainedXP += additionalXP;
        gainedXP = ApplyScalingFactor(gainedXP, currentLevel, victimLevel.Level._Value);

        if (steamId.TryGetPlayerPrestiges(out var prestiges) && prestiges.TryGetValue(PrestigeType.Experience, out var PrestigeData) && PrestigeData > 0)
        {
            int exoLevel = prestiges.TryGetValue(PrestigeType.Exo, out var exo) ? exo : 0;
            float expReductionFactor = 1 - LevelingPrestigeReducer * PrestigeData;

            if (exoLevel == 0)
            {
                gainedXP *= expReductionFactor;
            }
        }

        if (UnitSpawnerMultiplier < 1 && target.Has<IsMinion>() && target.Read<IsMinion>().Value)
        {
            gainedXP *= UnitSpawnerMultiplier;
            if (gainedXP == 0) return;
        }

        if (WarEventMultiplier < 1 && target.Has<SpawnBuffElement>())
        {
            var spawnBuffElement = target.ReadBuffer<SpawnBuffElement>();
            for (int i = 0; i < spawnBuffElement.Length; i++)
            {
                if (spawnBuffElement[i].Buff.Equals(WarEventTrash))
                {
                    gainedXP *= WarEventMultiplier;
                    break;
                }
            }
        }

        if (DocileUnitMultiplier < 1 && target.Has<AggroConsumer>() && !isVBlood)
        {
            if (target.Read<AggroConsumer>().AlertDecayPerSecond == 99)
            {
                gainedXP *= DocileUnitMultiplier;
            }
        }

        gainedXP *= groupMultiplier;
        int rested = 0;

        if (RestedXPSystem) gainedXP = AddRestedXP(steamId, gainedXP, ref rested);

        SaveExperience(steamId, gainedXP);
        CheckAndHandleLevelUp(source, steamId, gainedXP, currentLevel, rested);
    }
    static float AddRestedXP(ulong steamId, float gainedXP, ref int rested)
    {
        if (steamId.TryGetPlayerRestedXP(out var restedData) && restedData.Value > 0)
        {
            float restedXP = restedData.Value;
            float bonusXP = Math.Min(gainedXP, restedXP);
            float totalXP = gainedXP + bonusXP;
            restedXP -= bonusXP;

            steamId.SetPlayerRestedXP(new KeyValuePair<DateTime, float>(restedData.Key, restedXP));
            rested = (int)bonusXP;

            return totalXP;
        }

        return gainedXP;
    }
    public static void ProcessQuestExperienceGain(User user, int multiplier)
    {
        ulong steamId = user.PlatformId;
        Entity character = user.LocalCharacter._Entity;

        int currentLevel = steamId.TryGetPlayerExperience(out var xpData) ? xpData.Key : 0;
        float gainedXP = ConvertLevelToXp(currentLevel) * 0.03f * multiplier;

        SaveExperience(steamId, gainedXP);
        CheckAndHandleLevelUp(character, steamId, gainedXP, currentLevel);
    }
    static bool IsVBlood(Entity target)
    {
        return target.Has<VBloodConsumeSource>();
    }
    static float GetBaseExperience(int targetLevel, bool isVBlood)
    {
        int baseXP = targetLevel;

        if (isVBlood) return baseXP * VBloodLevelingMultiplier;
        return baseXP * UnitLevelingMultiplier;
    }
    static void SaveExperience(ulong steamId, float gainedXP)
    {
        if (!steamId.TryGetPlayerExperience(out var xpData))
        {
            xpData = new KeyValuePair<int, float>(0, 0); // Initialize if not present
        }

        float newExperience = xpData.Value + gainedXP;
        int newLevel = ConvertXpToLevel(newExperience);

        if (newLevel > MaxPlayerLevel)
        {
            newLevel = MaxPlayerLevel; // Cap the level at the maximum
            newExperience = ConvertLevelToXp(MaxPlayerLevel); // Adjust the XP to the max level's XP
        }

        steamId.SetPlayerExperience(new KeyValuePair<int, float>(newLevel, newExperience));
    }
    static void CheckAndHandleLevelUp(Entity playerCharacter, ulong steamId, float gainedXP, int currentLevel, int restedXP = 0)
    {
        Entity userEntity = playerCharacter.Read<PlayerCharacter>().UserEntity;
        bool leveledUp = CheckForLevelUp(steamId, currentLevel);

        if (leveledUp)
        {
            BuffUtilities.TryApplyBuff(playerCharacter, LevelUpBuff);
            if (Classes) BuffUtilities.ApplyClassBuffs(playerCharacter, steamId);
        }

        NotifyPlayer(userEntity, steamId, (int)gainedXP, leveledUp, restedXP);
    }
    static bool CheckForLevelUp(ulong SteamID, int currentLevel)
    {
        int newLevel = ConvertXpToLevel(GetXp(SteamID));

        if (newLevel > currentLevel)
        {
            return true;
        }

        return false;
    }
    static void NotifyPlayer(Entity userEntity, ulong steamId, int gainedXP, bool leveledUp, int restedXP)
    {
        User user = userEntity.Read<User>();
        Entity character = user.LocalCharacter.GetEntityOnServer();

        if (leveledUp)
        {
            int newLevel = GetLevel(steamId);
            SetLevel(character);

            if (newLevel <= MaxPlayerLevel) LocalizationService.HandleServerReply(EntityManager, user, $"Congratulations, you've reached level <color=white>{newLevel}</color>!");
            if (PlayerUtilities.GetPlayerBool(steamId, "Reminders") && Classes && !ClassUtilities.HasClass(steamId))
            {
                LocalizationService.HandleServerReply(EntityManager, user, $"Don't forget to choose a class! Use <color=white>'.class l'</color> to view choices and see what they have to offer with <color=white>'.class lb [Class]'</color> (buffs), <color=white>'.class lsp [Class]'</color> (spells), and <color=white>'.class lst [Class]'</color> (synergies). (toggle reminders with <color=white>'.remindme'</color>)");
            }
        }

        if (PlayerUtilities.GetPlayerBool(steamId, "ExperienceLogging"))
        {
            //Core.Log.LogInfo($"Player {user.CharacterName.Value} gained {gainedXP} rested {restedXP} leveled up {leveledUp} progress {GetLevelProgress(SteamID)}");
            int levelProgress = GetLevelProgress(steamId);
            string message = restedXP > 0 ? $"+<color=yellow>{gainedXP}</color> <color=green>rested</color> <color=#FFC0CB>experience</color> (<color=white>{levelProgress}%</color>)" : $"+<color=yellow>{gainedXP}</color> <color=#FFC0CB>experience</color> (<color=white>{levelProgress}%</color>)";
            LocalizationService.HandleServerReply(EntityManager, user, message);
        }
    }
    public static int ConvertXpToLevel(float xp)
    {
        return (int)(EXP_CONSTANT * Math.Sqrt(xp));
    }
    public static int ConvertLevelToXp(int level)
    {
        return (int)Math.Pow(level / EXP_CONSTANT, EXP_POWER);
    }
    static float GetXp(ulong steamId)
    {
        if (steamId.TryGetPlayerExperience(out var xpData))
        {
            return xpData.Value;
        }

        return 0f;
    }
    public static int GetLevel(ulong steamId)
    {
        if (steamId.TryGetPlayerExperience(out var xpData))
        {
            return xpData.Key;
        }

        return 0;
    }
    static int GetLevelFromXp(ulong steamId)
    {
        return ConvertXpToLevel(GetXp(steamId));
    }
    public static int GetLevelProgress(ulong steamId)
    {
        float currentXP = GetXp(steamId);

        int currentLevelXP = ConvertLevelToXp(GetLevelFromXp(steamId));
        int nextLevelXP = ConvertLevelToXp(GetLevelFromXp(steamId) + 1);

        double neededXP = nextLevelXP - currentLevelXP;
        double earnedXP = nextLevelXP - currentXP;

        return 100 - (int)Math.Ceiling(earnedXP / neededXP * 100);
    }
    static float ApplyScalingFactor(float gainedXP, int currentLevel, int victimLevel)
    {
        float k = LevelScalingMultiplier;
        int levelDifference = currentLevel - victimLevel;
        if (k <= 0) return gainedXP;
        float scalingFactor = levelDifference > 0 ? MathF.Exp(-k * levelDifference) : 1.0f;
        return gainedXP * scalingFactor;
    }
    public static bool TryParseClassName(string className, out PlayerClass parsedClassType)
    {
        // Attempt to parse the className string to the PlayerClasses enum.
        if (Enum.TryParse(className, true, out parsedClassType))
        {
            return true; // Successfully parsed
        }

        // If the initial parse failed, try to find a matching PlayerClasses enum value containing the input string.
        parsedClassType = Enum.GetValues(typeof(PlayerClass))
                             .Cast<PlayerClass>()
                             .FirstOrDefault(ct => ct.ToString().Contains(className, StringComparison.OrdinalIgnoreCase));

        // Check if a valid enum value was found that contains the input string.
        if (!parsedClassType.Equals(default(PlayerClass)))
        {
            return true; // Found a matching enum value
        }

        // If no match is found, return false and set the out parameter to default value.
        parsedClassType = default;
        return false; // Parsing failed
    }
    public static void SetLevel(Entity player)
    {
        ulong steamId = player.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;

        if (steamId.TryGetPlayerExperience(out var xpData))
        {
            int playerLevel = xpData.Key;
            Equipment equipment = player.Read<Equipment>();

            equipment.ArmorLevel._Value = 0f;
            equipment.SpellLevel._Value = 0f;
            equipment.WeaponLevel._Value = playerLevel;
            player.Write(equipment);
        }
    }
    public static void UpdateMaxRestedXP(ulong steamId, KeyValuePair<int, float> expData)
    {
        if (steamId.TryGetPlayerRestedXP(out var restedData) && restedData.Value > 0)
        {
            float currentRestedXP = restedData.Value;

            int currentLevel = expData.Key;
            int maxRestedLevel = Math.Min(RestedXPMax + currentLevel, MaxPlayerLevel);
            float restedCap = ConvertLevelToXp(maxRestedLevel) - ConvertLevelToXp(currentLevel);

            currentRestedXP = Math.Min(currentRestedXP, restedCap);
            steamId.SetPlayerRestedXP(new KeyValuePair<DateTime, float>(restedData.Key, currentRestedXP));
        }
    }
}