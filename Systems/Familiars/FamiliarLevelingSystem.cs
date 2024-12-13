using Bloodcraft.Services;
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
using static Bloodcraft.Utilities.Progression;

namespace Bloodcraft.Systems.Familiars;
internal static class FamiliarLevelingSystem
{
    static EntityManager EntityManager => Core.EntityManager;
    static SystemService SystemService => Core.SystemService;
    static EndSimulationEntityCommandBufferSystem EndSimulationEntityCommandBufferSystem => SystemService.EndSimulationEntityCommandBufferSystem;

    static readonly float UnitFamiliarMultiplier = ConfigService.UnitFamiliarMultiplier;
    static readonly float VBloodFamiliarMultiplier = ConfigService.VBloodFamiliarMultiplier;
    static readonly int MaxFamiliarLevel = ConfigService.MaxFamiliarLevel;

    static readonly PrefabGUID LevelUpBuff = new(-1133938228);
    static readonly PrefabGUID InvulnerableBuff = new(-480024072);

    static readonly WaitForSeconds SCTDelay = new(0.75f);

    static readonly float3 Gold = new(1.0f, 0.8431373f, 0.0f); // Bright Gold
    static readonly AssetGuid AssetGuid = AssetGuid.FromString("4210316d-23d4-4274-96f5-d6f0944bd0bb"); // experience hexString key
    static readonly PrefabGUID FamiliarSCT = new(1876501183); // SCT resource gain prefabguid, good visibility
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

        if (!familiar.Exists() || familiar.IsDisabled() || familiar.HasBuff(InvulnerableBuff)) return; // don't process if familiar not found, not active, or not in combat mode

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

        KeyValuePair<int, float> familiarXP = GetFamiliarExperience(steamId, familiarId);

        if (familiarXP.Key >= MaxFamiliarLevel) return;

        int currentLevel = ConvertXpToLevel(familiarXP.Value);
        UpdateFamiliarExperience(player, familiar, familiarId, steamId, familiarXP, gainedXP, currentLevel);
    }
    static float CalculateExperienceGained(int victimLevel, bool isVBlood)
    {
        int baseXP = victimLevel;
        if (isVBlood) return baseXP * VBloodFamiliarMultiplier;
        return baseXP * UnitFamiliarMultiplier;
    }
    public static void UpdateFamiliarExperience(Entity player, Entity familiar, int familiarId, ulong playerId, KeyValuePair<int, float> familiarXP, float gainedXP, int currentLevel)
    {
        FamiliarExperienceData data = FamiliarExperienceManager.LoadFamiliarExperience(playerId);
        data.FamiliarExperience[familiarId] = new(familiarXP.Key, familiarXP.Value + gainedXP);

        FamiliarExperienceManager.SaveFamiliarExperience(playerId, data);
        CheckAndHandleLevelUp(player, familiar, familiarId, playerId, data.FamiliarExperience[familiarId], currentLevel, gainedXP);
    }
    public static KeyValuePair<int, float> GetFamiliarExperience(ulong playerId, int familiarId)
    {
        FamiliarExperienceData data = FamiliarExperienceManager.LoadFamiliarExperience(playerId);

        if (data.FamiliarExperience.TryGetValue(familiarId, out var familiarData))
        {
            return familiarData;
        }
        else
        {
            return new(0, 0);
        }
    }
    public static void CheckAndHandleLevelUp(Entity player, Entity familiar, int familiarId, ulong steamID, KeyValuePair<int, float> familiarXP, int currentLevel, float gainedXP)
    {
        Entity userEntity = player.GetUserEntity();

        bool leveledUp = false;
        int newLevel = ConvertXpToLevel(familiarXP.Value);

        if (newLevel > currentLevel)
        {
            leveledUp = true;
            FamiliarExperienceData data = FamiliarExperienceManager.LoadFamiliarExperience(steamID);

            data.FamiliarExperience[familiarId] = new(newLevel, familiarXP.Value);
            FamiliarExperienceManager.SaveFamiliarExperience(steamID, data);
        }

        if (leveledUp)
        {
            Buffs.TryApplyBuff(familiar, LevelUpBuff);

            UnitLevel unitLevel = familiar.Read<UnitLevel>();
            unitLevel.Level._Value = newLevel;
            familiar.Write(unitLevel);

            FamiliarSummonSystem.ModifyDamageStats(familiar, newLevel, steamID, familiarId);
            if (familiar.Has<BloodConsumeSource>()) FamiliarSummonSystem.ModifyBloodSource(familiar, newLevel);
        }

        if (Misc.GetPlayerBool(steamID, "ScrollingText"))
        {
            float3 targetPosition = familiar.Read<Translation>().Value;

            Core.StartCoroutine(DelayedProfessionSCT(player, userEntity, targetPosition, Gold, gainedXP));
        }
    }
    static IEnumerator DelayedProfessionSCT(Entity character, Entity userEntity, float3 position, float3 color, float gainedXP)
    {
        yield return SCTDelay;

        ScrollingCombatTextMessage.Create(EntityManager, EndSimulationEntityCommandBufferSystem.CreateCommandBuffer(), AssetGuid, position, color, character, gainedXP, FamiliarSCT, userEntity);
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