using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM;
using Stunlock.Core;
using System.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Bloodcraft.Services.DataService.FamiliarPersistence;

namespace Bloodcraft.Systems.Familiars;
internal static class FamiliarLevelingSystem
{
    static EntityManager EntityManager => Core.EntityManager;
    static SystemService SystemService => Core.SystemService;
    static EndSimulationEntityCommandBufferSystem EndSimulationEntityCommandBufferSystem => SystemService.EndSimulationEntityCommandBufferSystem;

    const float EXP_CONSTANT = 0.1f;
    const int EXP_POWER = 2;

    static readonly float UnitFamiliarMultiplier = ConfigService.UnitFamiliarMultiplier;
    static readonly float VBloodFamiliarMultiplier = ConfigService.VBloodFamiliarMultiplier;
    static readonly int MaxFamiliarLevel = ConfigService.MaxFamiliarLevel;

    static readonly PrefabGUID LevelUpBuff = new(-1133938228);
    static readonly PrefabGUID InvulnerableBuff = new(-480024072);

    static readonly WaitForSeconds SCTDelay = new(0.75f);

    static readonly float3 Gold = new(1.0f, 0.8431373f, 0.0f); // Bright Gold
    static readonly AssetGuid AssetGuid = AssetGuid.FromString("4210316d-23d4-4274-96f5-d6f0944bd0bb"); // experience hexString key
    static readonly PrefabGUID FamiliarSCT = new(1876501183); // SCT resource gain prefabguid

    /* probably makes more sense to do this in the player leveling system if familiars are enabled
    public static void OnUpdate(object sender, DeathEventArgs deathEvent)
    {
        ProcessFamiliarExperience(deathEvent.Source, deathEvent.Target);
    }
    */
    public static void ProcessFamiliarExperience(Entity source, Entity target, ulong steamId, float groupMultiplier)
    {
        Entity familiar = FamiliarUtilities.FindPlayerFamiliar(source);

        if (!familiar.Exists() || familiar.IsDisabled() || familiar.HasBuff(InvulnerableBuff)) return; // don't process if familiar not found, not active, or not in combat mode

        PrefabGUID familiarUnit = familiar.Read<PrefabGUID>();
        int familiarId = familiarUnit.GuidHash;

        ProcessExperienceGain(source, familiar, target, steamId, familiarId, groupMultiplier);
    }
    static void ProcessExperienceGain(Entity player,Entity familiar, Entity target, ulong steamId, int familiarId, float groupMultiplier)
    {
        UnitLevel victimLevel = target.Read<UnitLevel>();
        bool isVBlood = IsVBlood(target);

        float gainedXP = CalculateExperienceGained(victimLevel.Level, isVBlood);
        gainedXP *= groupMultiplier;

        KeyValuePair<int, float> familiarXP = GetFamiliarExperience(steamId, familiarId);

        if (familiarXP.Key >= MaxFamiliarLevel) return;

        int currentLevel = ConvertXpToLevel(familiarXP.Value);
        UpdateFamiliarExperience(player, familiar, familiarId, steamId, familiarXP, gainedXP, currentLevel);
    }
    static bool IsVBlood(Entity target)
    {
        return target.Has<VBloodConsumeSource>();
    }
    static float CalculateExperienceGained(int victimLevel, bool isVBlood)
    {
        int baseXP = victimLevel;
        if (isVBlood) return baseXP * VBloodFamiliarMultiplier;
        return baseXP * UnitFamiliarMultiplier;
    }
    static void UpdateFamiliarExperience(Entity player, Entity familiar, int familiarId, ulong playerId, KeyValuePair<int, float> familiarXP, float gainedXP, int currentLevel)
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
    static void CheckAndHandleLevelUp(Entity player, Entity familiar, int familiarId, ulong steamID, KeyValuePair<int, float> familiarXP, int currentLevel, float gainedXP)
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
            BuffUtilities.TryApplyBuff(familiar, LevelUpBuff);

            UnitLevel unitLevel = familiar.Read<UnitLevel>();
            unitLevel.Level._Value = newLevel;
            familiar.Write(unitLevel);

            FamiliarSummonSystem.ModifyDamageStats(familiar, newLevel, steamID, familiarId);
            if (familiar.Has<BloodConsumeSource>()) FamiliarSummonSystem.ModifyBloodSource(familiar, newLevel);
        }

        if (PlayerUtilities.GetPlayerBool(steamID, "ScrollingText"))
        {
            float3 targetPosition = familiar.Read<Translation>().Value;

            Core.StartCoroutine(DelayedProfessionSCT(player, userEntity, targetPosition, Gold, gainedXP));
        }
    }
    static IEnumerator DelayedProfessionSCT(Entity character, Entity userEntity, float3 position, float3 color, float gainedXP)
    {
        yield return SCTDelay;

        Entity scrollingTextEntity = ScrollingCombatTextMessage.Create(EntityManager, EndSimulationEntityCommandBufferSystem.CreateCommandBuffer(), AssetGuid, position, color, character, gainedXP, FamiliarSCT, userEntity);
    }
    public static int ConvertXpToLevel(float xp)
    {
        // Assuming a basic square root scaling for experience to level conversion
        return (int)(EXP_CONSTANT * Math.Sqrt(xp));
    }
    public static int ConvertLevelToXp(int level)
    {
        // Reversing the formula used in ConvertXpToLevel for consistency
        return (int)Math.Pow(level / EXP_CONSTANT, EXP_POWER);
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