using Bloodcraft.Services;
using Bloodcraft.Systems.Leveling;
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

    static readonly float _unitFamiliarMultiplier = ConfigService.UnitFamiliarMultiplier;
    static readonly float _vBloodFamiliarMultiplier = ConfigService.VBloodFamiliarMultiplier;
    static readonly float _unitSpawnerMultiplier = ConfigService.UnitSpawnerMultiplier;
    static readonly float _levelingPrestigeReducer = ConfigService.LevelingPrestigeReducer;
    static readonly int _maxFamiliarLevel = ConfigService.MaxFamiliarLevel;

    static readonly PrefabGUID _levelUpBuff = new(-1133938228);

    static readonly WaitForSeconds _delay = new(0.75f);

    static readonly float3 _gold = new(1.0f, 0.8431373f, 0.0f); // Bright Gold
    static readonly AssetGuid _assetGuid = AssetGuid.FromString("4210316d-23d4-4274-96f5-d6f0944bd0bb"); // experience hexString key
    static readonly PrefabGUID _familiarSCT = new(1876501183); // SCT resource gain prefabguid, good visibility
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
        Entity familiar = Utilities.Familiars.FindPlayerFamiliar(source);

        if (!familiar.IsEligibleForCombat()) return;

        PrefabGUID familiarUnit = familiar.Read<PrefabGUID>();
        int familiarId = familiarUnit.GuidHash;

        ProcessExperienceGain(source, familiar, target, steamId, familiarId, groupMultiplier);
    }
    static void ProcessExperienceGain(Entity player, Entity familiar, Entity target, ulong steamId, int familiarId, float groupMultiplier)
    {
        UnitLevel victimLevel = target.Read<UnitLevel>();
        bool isVBlood = target.IsVBlood();

        float gainedXP = CalculateExperienceGained(victimLevel.Level, isVBlood);
        gainedXP *= groupMultiplier;

        if (_unitSpawnerMultiplier < 1 && target.TryGetComponent(out IsMinion isMinion) && isMinion.Value)
        {
            gainedXP *= _unitSpawnerMultiplier;
            if (gainedXP == 0) return;
        }

        if (steamId.TryGetPlayerPrestiges(out var prestiges) && prestiges.TryGetValue(PrestigeType.Experience, out var PrestigeData) && PrestigeData > 0)
        {
            int exoLevel = prestiges.TryGetValue(PrestigeType.Exo, out var exo) ? exo : 0;
            float expReductionFactor = 1 - _levelingPrestigeReducer * PrestigeData;

            if (exoLevel == 0)
            {
                gainedXP *= expReductionFactor;
            }
        }

        KeyValuePair<int, float> familiarXP = GetFamiliarExperience(steamId, familiarId);

        if (familiarXP.Key >= _maxFamiliarLevel) return;

        int currentLevel = ConvertXpToLevel(familiarXP.Value);
        UpdateFamiliarExperience(player, familiar, familiarId, steamId, familiarXP, gainedXP, currentLevel);
    }
    static float CalculateExperienceGained(int victimLevel, bool isVBlood)
    {
        int baseXP = victimLevel;
        if (isVBlood) return baseXP * _vBloodFamiliarMultiplier;
        return baseXP * _unitFamiliarMultiplier;
    }
    public static void UpdateFamiliarExperience(Entity player, Entity familiar, int familiarId, ulong playerId, KeyValuePair<int, float> familiarXP, float gainedXP, int currentLevel)
    {
        FamiliarExperienceData data = FamiliarExperienceManager.LoadFamiliarExperienceData(playerId);
        data.FamiliarLevels[familiarId] = new(familiarXP.Key, familiarXP.Value + gainedXP);

        FamiliarExperienceManager.SaveFamiliarExperienceData(playerId, data);
        CheckAndHandleLevelUp(player, familiar, familiarId, playerId, data.FamiliarLevels[familiarId], currentLevel, gainedXP);
    }
    public static KeyValuePair<int, float> GetFamiliarExperience(ulong playerId, int familiarId)
    {
        FamiliarExperienceData data = FamiliarExperienceManager.LoadFamiliarExperienceData(playerId);

        if (data.FamiliarLevels.TryGetValue(familiarId, out var familiarData))
        {
            return familiarData;
        }
        else
        {
            return new(0, 0);
        }
    }
    public static void CheckAndHandleLevelUp(Entity player, Entity familiar, int familiarId, ulong steamId, KeyValuePair<int, float> familiarXP, int currentLevel, float gainedXP)
    {
        Entity userEntity = player.GetUserEntity();

        bool leveledUp = false;
        int newLevel = ConvertXpToLevel(familiarXP.Value);

        if (newLevel > currentLevel)
        {
            leveledUp = true;
            FamiliarExperienceData data = FamiliarExperienceManager.LoadFamiliarExperienceData(steamId);

            data.FamiliarLevels[familiarId] = new(newLevel, familiarXP.Value);
            FamiliarExperienceManager.SaveFamiliarExperienceData(steamId, data);
        }

        if (leveledUp)
        {
            Buffs.TryApplyBuff(familiar, _levelUpBuff);

            UnitLevel unitLevel = familiar.Read<UnitLevel>();
            unitLevel.Level._Value = newLevel;
            familiar.Write(unitLevel);

            FamiliarSummonSystem.ModifyUnitStats(familiar, newLevel, steamId, familiarId);
            if (familiar.Has<BloodConsumeSource>()) FamiliarSummonSystem.ModifyBloodSource(familiar, newLevel);
        }

        if (GetPlayerBool(steamId, "ScrollingText"))
        {
            float3 targetPosition = familiar.Read<Translation>().Value;

            FamiliarExperienceSCTDelayRoutine(player, userEntity, targetPosition, _gold, gainedXP).Start();
        }
    }
    static IEnumerator FamiliarExperienceSCTDelayRoutine(Entity character, Entity userEntity, float3 position, float3 color, float gainedXP)
    {
        yield return _delay;
        
        ScrollingCombatTextMessage.Create(EntityManager, EndSimulationEntityCommandBufferSystem.CreateCommandBuffer(), _assetGuid, position, color, character, gainedXP, _familiarSCT, userEntity);
    }
    static float GetXp(ulong steamID, int familiarId)
    {
        return GetFamiliarExperience(steamID, familiarId).Value;
    }
    static int GetLevel(ulong SteamID, int familiarId)
    {
        return ConvertXpToLevel(GetXp(SteamID, familiarId));
    }
    public static int GetLevelProgress(ulong SteamID, int familiarId)
    {
        float currentXP = GetXp(SteamID, familiarId);
        int currentLevelXP = ConvertLevelToXp(GetLevel(SteamID, familiarId));
        int nextLevelXP = ConvertLevelToXp(GetLevel(SteamID, familiarId) + 1);

        double neededXP = nextLevelXP - currentLevelXP;
        double earnedXP = nextLevelXP - currentXP;

        return 100 - (int)Math.Ceiling(earnedXP / neededXP * 100);
    }
}