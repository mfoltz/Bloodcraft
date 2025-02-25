﻿using Bloodcraft.Services;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Utilities;
using ProjectM;
using Stunlock.Core;
using System.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Bloodcraft.Patches.DeathEventListenerSystemPatch;
using static Bloodcraft.Utilities.Misc.PlayerBoolsManager;
using static Bloodcraft.Utilities.Progression;
using User = ProjectM.Network.User;

namespace Bloodcraft.Systems.Leveling;
internal static class LevelingSystem
{
    static EntityManager EntityManager => Core.EntityManager;
    static SystemService SystemService => Core.SystemService;
    static EndSimulationEntityCommandBufferSystem EndSimulationEntityCommandBufferSystem => SystemService.EndSimulationEntityCommandBufferSystem;

    static readonly bool _classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;
    static readonly bool _familiars = ConfigService.FamiliarSystem;
    static readonly bool _restedXPSystem = ConfigService.RestedXPSystem;

    static readonly int _maxPlayerLevel = ConfigService.MaxLevel;
    static readonly float _groupMultiplier = ConfigService.GroupLevelingMultiplier;
    static readonly int _restedXPMax = ConfigService.RestedXPMax;

    static readonly float _unitSpawnerMultiplier = ConfigService.UnitSpawnerMultiplier;
    static readonly float _warEventMultiplier = ConfigService.WarEventMultiplier;
    static readonly float _docileUnitMultiplier = ConfigService.DocileUnitMultiplier;
    static readonly float _levelScalingMultiplier = ConfigService.LevelScalingMultiplier;

    static readonly float _vBloodLevelingMultiplier = ConfigService.VBloodLevelingMultiplier;
    static readonly float _unitLevelingMultiplier = ConfigService.UnitLevelingMultiplier;

    static readonly float _levelingPrestigeReducer = ConfigService.LevelingPrestigeReducer;

    static readonly WaitForSeconds _delay = new(0.75f);

    static readonly PrefabGUID _levelUpBuff = new(-1133938228);
    static readonly PrefabGUID _warEventTrash = new(2090187901);

    static readonly float3 _gold = new(1.0f, 0.8431373f, 0.0f); // Bright Gold
    static readonly AssetGuid _experienceAssetGuid = AssetGuid.FromString("4210316d-23d4-4274-96f5-d6f0944bd0bb");
    static readonly PrefabGUID _sctResourceGain = new(1876501183);

    // public static readonly HashSet<ulong> PartiedPlayers = [];

    static readonly HashSet<PrefabGUID> _extraGearLevelBuffs =
    [
        new(-1567599344), // SetBonus_PhysicalPower_GearLevel_01
        new(244750581),   // SetBonus_GearLevel_02
        new(-1469378405) // SetBonus_GearLevel_01
        // new(-1596803256)  // AB_BloodBuff_Brute_GearLevelBonus
    ];

    static readonly PrefabGUID _bruteGearLevelBuff = new(-1596803256);
    public static void OnUpdate(object sender, DeathEventArgs deathEvent)
    {
        ProcessExperience(deathEvent.Target, deathEvent.DeathParticipants);
    }
    public static void ProcessExperience(Entity target, HashSet<Entity> deathParticipants)
    {
        float groupMultiplier = 1f;
        bool inGroup = deathParticipants.Count > 1;

        if (inGroup) groupMultiplier = _groupMultiplier; // if more than 1 participant, apply group multiplier

        foreach (Entity playerCharacter in deathParticipants)
        {
            ulong steamId = playerCharacter.GetSteamId();

            if (_familiars) FamiliarLevelingSystem.ProcessFamiliarExperience(playerCharacter, target, steamId, groupMultiplier);

            int currentLevel = steamId.TryGetPlayerExperience(out var xpData) ? xpData.Key : 0;
            bool maxLevel = currentLevel >= _maxPlayerLevel;

            if (maxLevel && inGroup) // if at max level, prestige or no, and in a group (party or clan) get expertise exp instead
            {
                WeaponSystem.ProcessExpertise(playerCharacter, target, groupMultiplier);
            }
            else if (maxLevel) return;
            else ProcessExperienceGain(playerCharacter, target, steamId, currentLevel, groupMultiplier);
        }
    }
    public static void ProcessExperienceGain(Entity playerCharacter, Entity target, ulong steamId, int currentLevel, float groupMultiplier = 1f)
    {
        UnitLevel victimLevel = target.Read<UnitLevel>();
        Health health = target.Read<Health>();

        bool isVBlood = target.IsVBlood();

        int additionalXP = (int)(health.MaxHealth._Value / 2.5f);
        float gainedXP = GetBaseExperience(victimLevel.Level._Value, isVBlood);

        gainedXP += additionalXP;
        gainedXP = ApplyScalingFactor(gainedXP, currentLevel, victimLevel.Level._Value);

        if (steamId.TryGetPlayerPrestiges(out var prestiges) && prestiges.TryGetValue(PrestigeType.Experience, out var PrestigeData) && PrestigeData > 0)
        {
            int exoLevel = prestiges.TryGetValue(PrestigeType.Exo, out var exo) ? exo : 0;
            float expReductionFactor = 1 - _levelingPrestigeReducer * PrestigeData;

            if (exoLevel == 0)
            {
                gainedXP *= expReductionFactor;
            }
        }

        if (_unitSpawnerMultiplier < 1 && target.IsUnitSpawnerSpawned())
        {
            // bool inParty = PartiedPlayers.Contains(steamId);
            // if (inParty || gainedXP <= 0) return;

            gainedXP *= _unitSpawnerMultiplier;
            if (gainedXP <= 0) return;
        }

        if (_warEventMultiplier < 1 && target.Has<SpawnBuffElement>())
        {
            var spawnBuffElement = target.ReadBuffer<SpawnBuffElement>();

            for (int i = 0; i < spawnBuffElement.Length; i++)
            {
                if (spawnBuffElement[i].Buff.Equals(_warEventTrash))
                {
                    gainedXP *= _warEventMultiplier;
                    break;
                }
            }
        }

        if (_docileUnitMultiplier < 1f && !isVBlood && target.TryGetComponent(out AggroConsumer aggroConsumer))
        {
            if (aggroConsumer.AlertDecayPerSecond == 99f)
            {
                gainedXP *= _docileUnitMultiplier;
            }
        }

        gainedXP *= groupMultiplier;
        int rested = 0;

        if (_restedXPSystem) gainedXP = AddRestedXP(steamId, gainedXP, ref rested);
        SaveLevelingExperience(steamId, gainedXP, out bool leveledUp, out int newLevel);

        if (leveledUp) HandlePlayerLevelUpEffects(playerCharacter, steamId);
        NotifyPlayer(playerCharacter, steamId, (int)gainedXP, leveledUp, newLevel, rested);
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
    static float GetBaseExperience(int targetLevel, bool isVBlood)
    {
        int baseXP = targetLevel;

        if (isVBlood) return baseXP * _vBloodLevelingMultiplier;
        return baseXP * _unitLevelingMultiplier;
    }
    public static void SaveLevelingExperience(ulong steamId, float gainedXP, out bool leveledUp, out int newLevel)
    {
        if (!steamId.TryGetPlayerExperience(out var xpData))
        {
            xpData = new KeyValuePair<int, float>(0, 0);
        }

        int oldLevel = xpData.Key;
        float currentXP = xpData.Value;

        float newExperience = currentXP + gainedXP;
        newLevel = ConvertXpToLevel(newExperience);

        leveledUp = false;

        if (newLevel > _maxPlayerLevel)
        {
            newLevel = _maxPlayerLevel;
            newExperience = ConvertLevelToXp(_maxPlayerLevel);
        }

        if (newLevel > oldLevel)
        {
            leveledUp = true;
        }

        steamId.SetPlayerExperience(new KeyValuePair<int, float>(newLevel, newExperience));
    }
    static void HandlePlayerLevelUpEffects(Entity playerCharacter, ulong steamId)
    {
        Buffs.TryApplyBuff(playerCharacter, _levelUpBuff);

        if (_classes)
        {
            Classes.ApplyClassBuffs(playerCharacter, steamId);
        }
    }
    public static void NotifyPlayer(Entity playerCharacter, ulong steamId, float gainedXP, bool leveledUp, int newLevel, int restedXP = 0)
    {
        int gainedIntXP = (int)gainedXP;

        Entity userEntity = playerCharacter.GetUserEntity();
        User user = userEntity.GetUser();

        if (newLevel >= _maxPlayerLevel) return;

        if (leveledUp)
        {
            SetLevel(playerCharacter);

            if (newLevel <= _maxPlayerLevel)
            {
                LocalizationService.HandleServerReply(EntityManager, user,
                    $"Congratulations, you've reached level <color=white>{newLevel}</color>!");
            }

            if (_classes && GetPlayerBool(steamId, REMINDERS_KEY) && !Classes.HasClass(steamId))
            {
                LocalizationService.HandleServerReply(EntityManager, user,
                    $"Don't forget to choose a class! Use <color=white>'.class l'</color> to view choices and see what they have to offer with <color=white>'.class lb [Class]'</color> (buffs), <color=white>'.class lsp [Class]'</color> (spells), and <color=white>'.class lst [Class]'</color> (synergies). (toggle reminders with <color=white>'.misc remindme'</color>)");
            }
        }

        if (GetPlayerBool(steamId, EXPERIENCE_LOG_KEY))
        {
            int levelProgress = GetLevelProgress(steamId);
            string message = restedXP > 0
                ? $"+<color=yellow>{gainedIntXP}</color> <color=green>rested</color> <color=#FFC0CB>experience</color> (<color=white>{levelProgress}%</color>)"
                : $"+<color=yellow>{gainedIntXP}</color> <color=#FFC0CB>experience</color> (<color=white>{levelProgress}%</color>)";

            LocalizationService.HandleServerReply(EntityManager, user, message);
        }

        if (GetPlayerBool(steamId, SCT_PLAYER_KEY))
        {
            // float3 targetPosition = character.Read<Translation>().Value;
            PlayerExperienceSCTDelayRoutine(playerCharacter, userEntity, _gold, gainedXP).Start();
        }
    }
    static IEnumerator PlayerExperienceSCTDelayRoutine(Entity playerCharacter, Entity userEntity, float3 color, float gainedXP) // maybe just have one of these in progression utilities but later
    {
        yield return _delay;

        float3 position = playerCharacter.GetPosition();
        ScrollingCombatTextMessage.Create(EntityManager, EndSimulationEntityCommandBufferSystem.CreateCommandBuffer(), _experienceAssetGuid, position, color, playerCharacter, gainedXP, _sctResourceGain, userEntity);
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
        float k = _levelScalingMultiplier;
        int levelDifference = currentLevel - victimLevel;
        if (k <= 0) return gainedXP;
        float scalingFactor = levelDifference > 0 ? MathF.Exp(-k * levelDifference) : 1.0f;
        return gainedXP * scalingFactor;
    }
    public static void SetLevel(Entity playerCharacter)
    {
        ulong steamId = playerCharacter.GetSteamId();

        if (steamId.TryGetPlayerExperience(out var xpData) && playerCharacter.Has<Equipment>())
        {
            int playerLevel = xpData.Key;

            HandleExtraLevels(playerCharacter, ref playerLevel);

            playerCharacter.With((ref Equipment equipment) =>
            {
                equipment.ArmorLevel._Value = 0f;
                equipment.SpellLevel._Value = 0f;
                equipment.WeaponLevel._Value = playerLevel;
            });
        }
    }
    static void HandleExtraLevels(Entity playerCharacter, ref int playerLevel)
    {
        if (playerCharacter.HasBuff(_bruteGearLevelBuff))
        {
            playerLevel++;
        }

        if (_extraGearLevelBuffs.Any(buff => playerCharacter.HasBuff(buff)))
        {
            playerLevel++;
        }
    }
    public static void UpdateMaxRestedXP(ulong steamId, KeyValuePair<int, float> expData)
    {
        if (steamId.TryGetPlayerRestedXP(out var restedData) && restedData.Value > 0)
        {
            float currentRestedXP = restedData.Value;
            int currentLevel = expData.Key;

            int maxRestedLevel = Math.Min(_restedXPMax + currentLevel, _maxPlayerLevel);
            float restedCap = ConvertLevelToXp(maxRestedLevel) - ConvertLevelToXp(currentLevel);

            currentRestedXP = Math.Min(currentRestedXP, restedCap);
            steamId.SetPlayerRestedXP(new KeyValuePair<DateTime, float>(restedData.Key, currentRestedXP));
        }
    }
}