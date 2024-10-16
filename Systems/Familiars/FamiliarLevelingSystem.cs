using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
using static Bloodcraft.Patches.DeathEventListenerSystemPatch;
using static Bloodcraft.Services.DataService.FamiliarPersistence;

namespace Bloodcraft.Systems.Familiars;
internal static class FamiliarLevelingSystem
{
    static SystemService SystemService => Core.SystemService;

    const float EXPConstant = 0.1f;
    const int EXPPower = 2;

    static readonly PrefabGUID LevelUpBuff = new(-1133938228);
    public static void OnUpdate(object sender, DeathEventArgs deathEvent)
    {
        ProcessFamiliarExperience(deathEvent.Source, deathEvent.Target);
    }
    public static void ProcessFamiliarExperience(Entity player, Entity victimEntity)
    {
        if (!player.Has<PlayerCharacter>()) return;
        PlayerCharacter playerCharacter = player.Read<PlayerCharacter>();
        Entity userEntity = playerCharacter.UserEntity;

        ulong steamId = userEntity.Read<User>().PlatformId;

        if (steamId.TryGetFamiliarActives(out var actives) && actives.Familiar.Exists()) return; // don't process if familiar not out

        Entity familiar = FamiliarUtilities.FindPlayerFamiliar(player);
        if (!familiar.Exists()) return; // don't process if familiar not found

        if (familiar.Has<Aggroable>() && !familiar.Read<Aggroable>().Value._Value) return; // don't process if familiar combat disabled

        PrefabGUID familiarUnit = familiar.Read<PrefabGUID>();
        int familiarId = familiarUnit.GuidHash;
        ProcessExperienceGain(familiar, victimEntity, steamId, familiarId);
    }
    static void ProcessExperienceGain(Entity familiarEntity, Entity victimEntity, ulong steamID, int familiarId)
    {
        UnitLevel victimLevel = victimEntity.Read<UnitLevel>();
        bool isVBlood = IsVBlood(victimEntity);

        float gainedXP = CalculateExperienceGained(victimLevel.Level, isVBlood);
        KeyValuePair<int, float> familiarXP = GetFamiliarExperience(steamID, familiarId);

        if (familiarXP.Key >= ConfigService.MaxFamiliarLevel) return;

        int currentLevel = ConvertXpToLevel(familiarXP.Value);

        UpdateFamiliarExperience(familiarEntity, familiarId, steamID, familiarXP, gainedXP, currentLevel);
    }
    static bool IsVBlood(Entity victimEntity)
    {
        return victimEntity.Has<VBloodConsumeSource>();
    }
    static float CalculateExperienceGained(int victimLevel, bool isVBlood)
    {
        int baseXP = victimLevel;
        if (isVBlood) return baseXP * ConfigService.VBloodFamiliarMultiplier;
        return baseXP * ConfigService.UnitFamiliarMultiplier;
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
    static void CheckAndHandleLevelUp(Entity familiar, int familiarId, ulong steamID, KeyValuePair<int, float> familiarXP, int currentLevel)
    {
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