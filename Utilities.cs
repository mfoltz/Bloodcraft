using Bloodcraft.Commands;
using Bloodcraft.Services;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Systems.Quests;
using ProjectM;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Bloodcraft.Core.DataStructures;

namespace Bloodcraft;
internal static class Utilities
{
    static EntityManager EntityManager => Core.EntityManager;
    

    public static IEnumerable<Entity> GetEntitiesEnumerable(EntityQuery entityQuery, bool checkBuffBuffer = false) // not sure if need to actually check for empty buff buffer for quest targets but don't really want to find out
    {
        JobHandle handle = GetEntities(entityQuery, out NativeArray<Entity> entities, Allocator.TempJob);
        handle.Complete();
        try
        {
            foreach (Entity entity in entities)
            {
                if (EntityManager.Exists(entity))
                {
                    if (checkBuffBuffer && entity.ReadBuffer<BuffBuffer>().IsEmpty) continue;
                    yield return entity;
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
    static JobHandle GetEntities(EntityQuery entityQuery, out NativeArray<Entity> entities, Allocator allocator = Allocator.TempJob)
    {
        entities = entityQuery.ToEntityArray(allocator);
        return default;
    }
    public static bool GetPlayerBool(ulong steamId, string boolKey)
    {
        if (PlayerBools.ContainsKey(steamId)) return PlayerBools[steamId][boolKey];
        return false;
    }
    public static void TogglePlayerBool(ulong steamId, string boolKey)
    {
        if (PlayerBools.ContainsKey(steamId))
        {
            PlayerBools[steamId][boolKey] = !PlayerBools[steamId][boolKey];
            SavePlayerBools();
        }
    }
    public static void SetPlayerBool(ulong steamId, string boolKey, bool value)
    {
        if (PlayerBools.ContainsKey(steamId))
        {
            PlayerBools[steamId][boolKey] = value;
            SavePlayerBools();
        }
    }
    public static bool HasDismissed(ulong steamId, out Entity familiar)
    {
        familiar = Entity.Null;
        if (FamiliarActives.ContainsKey(steamId) && FamiliarActives[steamId].Familiar.Exists())
        {
            familiar = FamiliarActives[steamId].Familiar;
            return true;
        }
        return false;
    }
    public static void ReturnFamiliar(Entity player, Entity familiar)
    {
        Follower following = familiar.Read<Follower>();
        following.ModeModifiable._Value = 1;
        familiar.Write(following);

        float3 playerPos = player.Read<Translation>().Value;
        float distance = UnityEngine.Vector3.Distance(familiar.Read<Translation>().Value, playerPos);

        if (distance > 25f)
        {
            familiar.Write(new LastTranslation { Value = playerPos });
            familiar.Write(new Translation { Value = playerPos });
        }
    }
    public static void QuestRewards()
    {
        List<PrefabGUID> questRewards = ParseConfigString(ConfigService.QuestRewards).Select(x => new PrefabGUID(x)).ToList();
        List<int> rewardAmounts = [.. ParseConfigString(ConfigService.QuestRewardAmounts)];
        QuestSystem.QuestRewards = questRewards.Zip(rewardAmounts, (reward, amount) => new { reward, amount }).ToDictionary(x => x.reward, x => x.amount);
    }
    public static void StarterKit()
    {
        List<PrefabGUID> kitPrefabs = ParseConfigString(ConfigService.KitPrefabs).Select(x => new PrefabGUID(x)).ToList();
        List<int> kitAmounts = [.. ParseConfigString(ConfigService.KitQuantities)];
        MiscCommands.KitPrefabs = kitPrefabs.Zip(kitAmounts, (item, amount) => new { item, amount }).ToDictionary(x => x.item, x => x.amount);
    }
    public static void FamiliarBans()
    {
        List<int> unitBans = ParseConfigString(ConfigService.BannedUnits);
        List<string> typeBans = ConfigService.BannedTypes.Split(',').Select(s => s.Trim()).ToList();
        if (unitBans.Count > 0) FamiliarUnlockSystem.ExemptPrefabs = unitBans;
        if (typeBans.Count > 0) FamiliarUnlockSystem.ExemptTypes = typeBans;
    }
    public static List<int> ParseConfigString(string configString)
    {
        if (string.IsNullOrEmpty(configString))
        {
            return [];
        }
        return configString.Split(',').Select(int.Parse).ToList();
    }
}
