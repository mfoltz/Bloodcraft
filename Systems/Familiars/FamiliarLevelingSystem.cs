﻿using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM;
using Stunlock.Core;
using System.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Bloodcraft.Patches.DeathEventListenerSystemPatch;
using static Bloodcraft.Services.DataService.FamiliarPersistence;
using static Bloodcraft.Utilities.Misc.PlayerBoolsManager;
using static Bloodcraft.Utilities.Progression;

namespace Bloodcraft.Systems.Familiars;
internal static class FamiliarLevelingSystem
{
    static EntityManager EntityManager => Core.EntityManager;
    static SystemService SystemService => Core.SystemService;
    static EndSimulationEntityCommandBufferSystem EndSimulationEntityCommandBufferSystem => SystemService.EndSimulationEntityCommandBufferSystem;

    static readonly bool _familiarPrestige = ConfigService.FamiliarPrestige;

    static readonly float _unitFamiliarMultiplier = ConfigService.UnitFamiliarMultiplier;
    static readonly float _vBloodFamiliarMultiplier = ConfigService.VBloodFamiliarMultiplier;
    static readonly float _unitSpawnerMultiplier = ConfigService.UnitSpawnerMultiplier;
    static readonly float _levelingPrestigeReducer = ConfigService.LevelingPrestigeReducer;
    static readonly int _maxFamiliarLevel = ConfigService.MaxFamiliarLevel;

    static readonly PrefabGUID _levelUpBuff = new(-1133938228);

    static readonly WaitForSeconds _delay = new(3f); // try 1.5f? 0.75f old value, actually try 3 since needs to be after first

    static readonly float3 _gold = new(1.0f, 0.8431373f, 0.0f);
    static readonly AssetGuid _experienceAssetGuid = AssetGuid.FromString("4210316d-23d4-4274-96f5-d6f0944bd0bb");
    static readonly PrefabGUID _sctResourceGain = new(1876501183);
    public static void OnUpdate(object sender, DeathEventArgs deathEvent)
    {
        foreach (Entity player in deathEvent.DeathParticipants)
        {
            ulong steamId = player.GetSteamId();

            ProcessFamiliarExperience(player, deathEvent.Target, steamId, 1f);
        }
    }
    public static void ProcessFamiliarExperience(Entity source, Entity target, ulong steamId, float groupMultiplier)
    {
        Entity familiar = Utilities.Familiars.GetActiveFamiliar(source);

        if (!familiar.EligibleForCombat()) return;

        PrefabGUID familiarId = familiar.Read<PrefabGUID>();
        ProcessExperienceGain(source, familiar, target, steamId, familiarId.GuidHash, groupMultiplier);
    }
    static void ProcessExperienceGain(Entity player, Entity familiar, Entity target, ulong steamId, int famKey, float groupMultiplier)
    {
        int unitLevel = target.GetUnitLevel();
        bool isVBlood = target.IsVBlood();

        float gainedXP = CalculateExperienceGained(unitLevel, isVBlood);
        gainedXP *= groupMultiplier;

        if (_unitSpawnerMultiplier < 1 && target.TryGetComponent(out IsMinion isMinion) && isMinion.Value)
        {
            gainedXP *= _unitSpawnerMultiplier;

            if (gainedXP <= 0) return;
        }

        if (_familiarPrestige && FamiliarPrestigeManager_V2.LoadFamiliarPrestigeData_V2(steamId).FamiliarPrestige.TryGetValue(famKey, out var prestigeData) && prestigeData.Key > 0)
        {
            int prestiges = prestigeData.Key;
            float expReductionFactor = 1 - _levelingPrestigeReducer * prestiges;

            gainedXP *= expReductionFactor;
        }

        KeyValuePair<int, float> familiarXP = GetFamiliarExperience(steamId, famKey);

        if (familiarXP.Key >= _maxFamiliarLevel) return;

        int currentLevel = ConvertXpToLevel(familiarXP.Value);
        UpdateFamiliarExperience(player, familiar, famKey, steamId, familiarXP, gainedXP, currentLevel);
    }
    static float CalculateExperienceGained(int victimLevel, bool isVBlood)
    {
        int baseXP = victimLevel;
        if (isVBlood) return baseXP * _vBloodFamiliarMultiplier;
        return baseXP * _unitFamiliarMultiplier;
    }
    public static void UpdateFamiliarExperience(Entity player, Entity familiar, int famKey, ulong steamId, KeyValuePair<int, float> familiarXP, float gainedXP, int currentLevel)
    {
        FamiliarExperienceData data = FamiliarExperienceManager.LoadFamiliarExperienceData(steamId);
        data.FamiliarExperience[famKey] = new(familiarXP.Key, familiarXP.Value + gainedXP);

        FamiliarExperienceManager.SaveFamiliarExperienceData(steamId, data);
        CheckAndHandleLevelUp(player, familiar, famKey, steamId, data.FamiliarExperience[famKey], currentLevel, gainedXP);
    }
    public static KeyValuePair<int, float> GetFamiliarExperience(ulong steamId, int famKey)
    {
        FamiliarExperienceData data = FamiliarExperienceManager.LoadFamiliarExperienceData(steamId);

        if (data.FamiliarExperience.TryGetValue(famKey, out var familiarData))
        {
            return familiarData;
        }
        else
        {
            return new(FamiliarSummonSystem.BASE_LEVEL, ConvertLevelToXp(FamiliarSummonSystem.BASE_LEVEL));
        }
    }
    public static void CheckAndHandleLevelUp(Entity player, Entity familiar, int famKey, ulong steamId, KeyValuePair<int, float> familiarXP, int currentLevel, float gainedXP)
    {
        Entity userEntity = player.GetUserEntity();

        bool leveledUp = false;
        int newLevel = ConvertXpToLevel(familiarXP.Value);

        if (newLevel > currentLevel)
        {
            leveledUp = true;
            FamiliarExperienceData data = FamiliarExperienceManager.LoadFamiliarExperienceData(steamId);

            data.FamiliarExperience[famKey] = new(newLevel, familiarXP.Value);
            FamiliarExperienceManager.SaveFamiliarExperienceData(steamId, data);
        }

        if (leveledUp)
        {
            familiar.TryApplyBuff(_levelUpBuff);

            /*
            UnitLevel unitLevel = familiar.Read<UnitLevel>();
            unitLevel.Level._Value = newLevel;
            familiar.Write(unitLevel);
            */

            familiar.With((ref UnitLevel unitLevel) =>
            {
                unitLevel.Level._Value = newLevel;
            });

            FamiliarSummonSystem.ModifyUnitStats(familiar, newLevel, steamId, famKey);
            if (familiar.Has<BloodConsumeSource>()) FamiliarSummonSystem.ModifyBloodSource(familiar, newLevel);
        }

        if (GetPlayerBool(steamId, SCT_FAMILIAR_KEY))
        {
            // float3 targetPosition = familiar.Read<Translation>().Value;

            FamiliarExperienceSCTDelayRoutine(player, userEntity, familiar, _gold, gainedXP).Start();
        }
    }
    static IEnumerator FamiliarExperienceSCTDelayRoutine(Entity character, Entity userEntity, Entity familiar, float3 color, float gainedXP)
    {
        yield return _delay;
        
        float3 position = familiar.GetPosition();
        ScrollingCombatTextMessage.Create(EntityManager, EndSimulationEntityCommandBufferSystem.CreateCommandBuffer(), _experienceAssetGuid, position, color, character, gainedXP, _sctResourceGain, userEntity);
    }
    static float GetXp(ulong steamID, int familiarId)
    {
        return GetFamiliarExperience(steamID, familiarId).Value;
    }
    static int GetLevel(ulong SteamID, int familiarId)
    {
        return ConvertXpToLevel(GetXp(SteamID, familiarId));
    }
    public static int GetLevelProgress(ulong SteamID, int famKey)
    {
        float currentXP = GetXp(SteamID, famKey);
        int currentLevelXP = ConvertLevelToXp(GetLevel(SteamID, famKey));
        int nextLevelXP = ConvertLevelToXp(GetLevel(SteamID, famKey) + 1);

        double neededXP = nextLevelXP - currentLevelXP;
        double earnedXP = nextLevelXP - currentXP;

        return 100 - (int)Math.Ceiling(earnedXP / neededXP * 100);
    }
}