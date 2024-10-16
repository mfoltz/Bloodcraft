﻿using Bloodcraft.Services;
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

    const float EXPConstant = 0.1f; // constant for calculating level from xp
    const float EXPPower = 2f; // power for calculating level from xp

    static readonly bool Classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;

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
        ProcessExperience(deathEvent.Source, deathEvent.Target);
    }
    public static void ProcessExperience(Entity source, Entity target)
    {
        Entity userEntity = source.Read<PlayerCharacter>().UserEntity;
        ulong steamId = userEntity.Read<User>().PlatformId;

        if (IsVBlood(target))
        {
            ProcessExperienceGain(source, target, steamId, 1f); // override multiplier since this should just be a solo kill and skip getting participants for vbloods since they're all in the event list from VBloodSystem if involved in same kill
            return;
        }

        HashSet<Entity> participants = PlayerUtilities.GetDeathParticipants(source, userEntity); // want list of participants to process experience gains when appropriate
        int count = participants.Count;
        
        if (count > 1)
        {
            foreach (Entity player in participants)
            {
                steamId = player.GetSteamId(); // participants are character entities

                if (steamId.TryGetPlayerExperience(out var xpData) && xpData.Key >= ConfigService.MaxLevel) continue; // Check for max level before continuing
                ProcessExperienceGain(source, target, steamId, ConfigService.GroupLevelingMultiplier);
            }
        }
        else
        {
            if (steamId.TryGetPlayerExperience(out var xpData) && xpData.Key >= ConfigService.MaxLevel) return; // Check for max level before continuing
            ProcessExperienceGain(source, target, steamId, 1f);
        }
    }
    static void ProcessExperienceGain(Entity killerEntity, Entity victimEntity, ulong SteamID, float groupMultiplier)
    {
        UnitLevel victimLevel = victimEntity.Read<UnitLevel>();
        Health health = victimEntity.Read<Health>();

        bool isVBlood = IsVBlood(victimEntity);
        int additionalXP = (int)(health.MaxHealth._Value / 2.5f);
        float gainedXP = GetBaseExperience(victimLevel.Level._Value, isVBlood);

        gainedXP += additionalXP;
        int currentLevel = SteamID.TryGetPlayerExperience(out var xpData) ? xpData.Key : 0;

        if (currentLevel >= ConfigService.MaxLevel) return;

        gainedXP = ApplyScalingFactor(gainedXP, currentLevel, victimLevel.Level._Value);

        if (SteamID.TryGetPlayerPrestiges(out var prestiges) && prestiges.TryGetValue(PrestigeType.Experience, out var PrestigeData) && PrestigeData > 0)
        {
            int exoLevel = prestiges.TryGetValue(PrestigeType.Exo, out var exo) ? exo : 0;
            float expReductionFactor = 1 - ConfigService.LevelingPrestigeReducer * PrestigeData;
            if (exoLevel == 0)
            {
                gainedXP *= expReductionFactor;
            }
        }

        if (ConfigService.UnitSpawnerMultiplier < 1 && victimEntity.Has<IsMinion>() && victimEntity.Read<IsMinion>().Value)
        {
            gainedXP *= ConfigService.UnitSpawnerMultiplier;
            if (gainedXP == 0) return;
        }

        if (ConfigService.WarEventMultiplier < 1 && victimEntity.Has<SpawnBuffElement>())
        {
            var spawnBuffElement = victimEntity.ReadBuffer<SpawnBuffElement>();
            for (int i = 0; i < spawnBuffElement.Length; i++)
            {
                if (spawnBuffElement[i].Buff.Equals(WarEventTrash))
                {
                    gainedXP *= ConfigService.WarEventMultiplier;
                    break;
                }
            }
        }

        if (ConfigService.DocileUnitMultiplier < 1 && victimEntity.Has<AggroConsumer>() && !isVBlood)
        {
            if (victimEntity.Read<AggroConsumer>().AlertDecayPerSecond == 99)
            {
                gainedXP *= 0.2f;
            }
        }

        gainedXP *= groupMultiplier;
        int rested = 0;
        if (ConfigService.RestedXPSystem) gainedXP = AddRestedXP(SteamID, gainedXP, ref rested);

        SaveExperience(SteamID, gainedXP);
        CheckAndHandleLevelUp(killerEntity, victimEntity, SteamID, gainedXP, currentLevel, rested);
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
    public static void ProcessQuestExperienceGain(User user, Entity victim, int multiplier)
    {
        ulong SteamID = user.PlatformId;
        Entity character = user.LocalCharacter._Entity;
        int currentLevel = SteamID.TryGetPlayerExperience(out var xpData) ? xpData.Key : 0;
        float gainedXP = ConvertLevelToXp(currentLevel) * 0.03f * multiplier;

        SaveExperience(SteamID, gainedXP);
        CheckAndHandleLevelUp(character, victim, SteamID, gainedXP, currentLevel);
    }
    static bool IsVBlood(Entity victimEntity)
    {
        return victimEntity.Has<VBloodConsumeSource>();
    }
    static float GetBaseExperience(int victimLevel, bool isVBlood)
    {
        int baseXP = victimLevel;
        if (isVBlood) return baseXP * ConfigService.VBloodLevelingMultiplier;
        return baseXP * ConfigService.UnitLevelingMultiplier;
    }
    static void SaveExperience(ulong SteamID, float gainedXP)
    {
        if (!SteamID.TryGetPlayerExperience(out var xpData))
        {
            xpData = new KeyValuePair<int, float>(0, 0); // Initialize if not present
        }

        float newExperience = xpData.Value + gainedXP;
        int newLevel = ConvertXpToLevel(newExperience);

        if (newLevel > ConfigService.MaxLevel)
        {
            newLevel = ConfigService.MaxLevel; // Cap the level at the maximum
            newExperience = ConvertLevelToXp(ConfigService.MaxLevel); // Adjust the XP to the max level's XP
        }

        SteamID.SetPlayerExperience(new KeyValuePair<int, float>(newLevel, newExperience));
    }
    static void CheckAndHandleLevelUp(Entity playerCharacter, Entity target, ulong steamId, float gainedXP, int currentLevel, int restedXP = 0)
    {
        Entity userEntity = playerCharacter.Read<PlayerCharacter>().UserEntity;
        bool leveledUp = CheckForLevelUp(steamId, currentLevel);

        if (leveledUp)
        {
            BuffUtilities.TryApplyBuff(playerCharacter, LevelUpBuff);
            if (Classes) BuffUtilities.ApplyClassBuffs(playerCharacter, steamId);
        }

        NotifyPlayer(userEntity, target, steamId, (int)gainedXP, leveledUp, restedXP);
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
    static void NotifyPlayer(Entity userEntity, Entity victim, ulong steamId, int gainedXP, bool leveledUp, int restedXP)
    {
        User user = userEntity.Read<User>();
        Entity character = user.LocalCharacter._Entity;

        if (leveledUp)
        {
            int newLevel = GetLevel(steamId);
            SetLevel(character);

            if (newLevel <= ConfigService.MaxLevel) LocalizationService.HandleServerReply(EntityManager, user, $"Congratulations, you've reached level <color=white>{newLevel}</color>!");
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

        /*
        if (PlayerUtilities.GetPlayerBool(steamId, "ScrollingText"))
        {
            float3 position = victim.Read<Translation>().Value;
            float3 adjacentPosition = new(position.x + 0.05f, position.y - 0.05f, position.z - 0.01f);

            EntityCommandBuffer entityCommandBuffer = EntityCommandBufferSystem.CreateCommandBuffer();
            Entity sctEntity = ScrollingCombatTextMessage.Create(EntityManager, entityCommandBuffer, assetGuidLeveling, adjacentPosition, color, character, gainedXP, default, userEntity);
        }
        */
    }
    public static int ConvertXpToLevel(float xp)
    {
        return (int)(EXPConstant * Math.Sqrt(xp));
    }
    public static int ConvertLevelToXp(int level)
    {
        return (int)Math.Pow(level / EXPConstant, EXPPower);
    }
    static float GetXp(ulong SteamID)
    {
        if (SteamID.TryGetPlayerExperience(out var xpData))
        {
            return xpData.Value;
        }
        return 0f;
    }
    public static int GetLevel(ulong SteamID)
    {
        if (SteamID.TryGetPlayerExperience(out var xpData))
        {
            return xpData.Key;
        }
        return 0;
    }
    static int GetLevelFromXp(ulong SteamID)
    {
        return ConvertXpToLevel(GetXp(SteamID));
    }
    public static int GetLevelProgress(ulong SteamID)
    {
        float currentXP = GetXp(SteamID);
        int currentLevelXP = ConvertLevelToXp(GetLevelFromXp(SteamID));
        int nextLevelXP = ConvertLevelToXp(GetLevelFromXp(SteamID) + 1);

        double neededXP = nextLevelXP - currentLevelXP;
        double earnedXP = nextLevelXP - currentXP;

        return 100 - (int)Math.Ceiling(earnedXP / neededXP * 100);
    }
    static float ApplyScalingFactor(float gainedXP, int currentLevel, int victimLevel)
    {
        float k = ConfigService.LevelScalingMultiplier;
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
    public static void ResetRestedXP(ulong steamId)
    {
        if (steamId.TryGetPlayerRestedXP(out var restedData) && restedData.Value > 0)
        {
            restedData = new KeyValuePair<DateTime, float>(restedData.Key, 0);
            steamId.SetPlayerRestedXP(restedData);
        }
    }
}