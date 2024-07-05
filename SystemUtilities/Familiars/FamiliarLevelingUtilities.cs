using ProjectM;
using ProjectM.Network;
using Steamworks;
using Stunlock.Core;
using Unity.Entities;
using static Bloodcraft.Core;
using static Bloodcraft.Core.DataStructures;

namespace Bloodcraft.Systems.Familiars;
internal static class FamiliarLevelingUtilities
{
    static readonly float UnitMultiplier = Plugin.UnitFamiliarMultiplier.Value; // multipler for normal units
    static readonly float VBloodMultiplier = Plugin.VBloodFamiliarMultiplier.Value; // multiplier for VBlood units
    static readonly float EXPConstant = 0.1f; // constant for calculating level from xp
    static readonly int EXPPower = 2; // power for calculating level from xp
    static readonly int MaxFamiliarLevel = Plugin.MaxFamiliarLevel.Value; // maximum level

    static readonly PrefabGUID levelUpBuff = new(-1133938228);

    public static void UpdateFamiliar(Entity player, Entity victimEntity)
    {
        EntityManager entityManager = Core.EntityManager;
        if (!IsValidVictim(entityManager, victimEntity)) return;
        HandleExperienceUpdate(entityManager, player, victimEntity);
    }

    static bool IsValidVictim(EntityManager entityManager, Entity victimEntity)
    {
        return !entityManager.HasComponent<Minion>(victimEntity) && entityManager.HasComponent<UnitLevel>(victimEntity);
    }

    static void HandleExperienceUpdate(EntityManager entityManager, Entity player, Entity victimEntity)
    {
        if (!entityManager.HasComponent<PlayerCharacter>(player)) return;
        PlayerCharacter playerCharacter = entityManager.GetComponentData<PlayerCharacter>(player);
        Entity userEntity = playerCharacter.UserEntity;

        ulong steamId = userEntity.Read<User>().PlatformId;

        if (Core.DataStructures.FamiliarActives.TryGetValue(steamId, out var actives) && !actives.Familiar.Equals(Entity.Null)) return; // don't process if familiar not out

        Entity familiarEntity = FamiliarSummonUtilities.FamiliarUtilities.FindPlayerFamiliar(player);
        if (familiarEntity == Entity.Null || !Core.EntityManager.Exists(familiarEntity)) return;

        if (familiarEntity.Has<Aggroable>() && !familiarEntity.Read<Aggroable>().Value._Value) return; // don't process if familiar combat disabled

        PrefabGUID familiarUnit = familiarEntity.Read<PrefabGUID>();
        int familiarId = familiarUnit.GuidHash;
        ProcessExperienceGain(entityManager, familiarEntity, victimEntity, steamId, familiarId);
    }

    static void ProcessExperienceGain(EntityManager entityManager, Entity familiarEntity, Entity victimEntity, ulong steamID, int familiarId)
    {
        UnitLevel victimLevel = entityManager.GetComponentData<UnitLevel>(victimEntity);
        bool isVBlood = IsVBlood(entityManager, victimEntity);

        float gainedXP = CalculateExperienceGained(victimLevel.Level, isVBlood);
        KeyValuePair<int, float> familiarXP = GetFamiliarExperience(steamID, familiarId);

        int currentLevel = ConvertXpToLevel(familiarXP.Value);
        if (currentLevel >= MaxFamiliarLevel) return;

        UpdateFamiliarExperience(familiarEntity, familiarId, steamID, familiarXP, gainedXP, currentLevel);            
    }
    static bool IsVBlood(EntityManager entityManager, Entity victimEntity)
    {
        return entityManager.HasComponent<VBloodConsumeSource>(victimEntity);
    }
    static float CalculateExperienceGained(int victimLevel, bool isVBlood)
    {
        int baseXP = victimLevel;
        if (isVBlood) return baseXP * VBloodMultiplier;
        return baseXP * UnitMultiplier;
    }

    static void UpdateFamiliarExperience(Entity familiarEntity, int familiarId, ulong playerId, KeyValuePair<int, float> familiarXP, float gainedXP, int currentLevel)
    {
        FamiliarExperienceData data = FamiliarExperienceManager.LoadFamiliarExperience(playerId);
        data.FamiliarExperience[familiarId] = new(familiarXP.Key, familiarXP.Value + gainedXP);
        FamiliarExperienceManager.SaveFamiliarExperience(playerId, data);
        CheckAndHandleLevelUp(familiarEntity, familiarId, playerId, data.FamiliarExperience[familiarId], currentLevel);
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
    
    static void CheckAndHandleLevelUp(Entity familiarEntity, int familiarId, ulong steamID, KeyValuePair<int, float> familiarXP, int currentLevel)
    {
        Entity userEntity = familiarEntity.Read<Follower>().Followed._Value.Read<PlayerCharacter>().UserEntity;

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
            DebugEventsSystem debugEventsSystem = Core.DebugEventsSystem;
            ApplyBuffDebugEvent applyBuffDebugEvent = new()
            {
                BuffPrefabGUID = levelUpBuff,
            };
            FromCharacter fromCharacter = new()
            {
                Character = familiarEntity,
                User = userEntity,
            };
            int famKey = familiarEntity.Read<PrefabGUID>().GuidHash;
            debugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
            UnitLevel unitLevel = familiarEntity.Read<UnitLevel>();
            unitLevel.Level._Value = newLevel;
            familiarEntity.Write(unitLevel);

            int prestigeLevel = 0;
            if (Core.FamiliarPrestigeManager.LoadFamiliarPrestige(steamID).FamiliarPrestige.TryGetValue(familiarEntity.Read<PrefabGUID>().GuidHash, out var prestigeData) && prestigeData.Key > 0)
            {
                prestigeLevel = prestigeData.Key;
            }

            FamiliarSummonUtilities.ModifyDamageStats(familiarEntity, newLevel, steamID, famKey);
            if (familiarEntity.Has<BloodConsumeSource>()) FamiliarSummonUtilities.ModifyBloodSource(familiarEntity, newLevel);
        }
    }   
    public static int ConvertXpToLevel(float xp)
    {
        // Assuming a basic square root scaling for experience to level conversion
        return (int)(EXPConstant * Math.Sqrt(xp));
    }

    public static int ConvertLevelToXp(int level)
    {
        // Reversing the formula used in ConvertXpToLevel for consistency
        return (int)Math.Pow(level / EXPConstant, EXPPower);
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