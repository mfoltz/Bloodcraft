using ProjectM;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Bloodcraft.Utilities;

internal static class EntityUtilities
{
    static EntityManager EntityManager => Core.EntityManager;

    static readonly HashSet<string> FilteredTargetNames =
    [
        "Trader",
        "HostileVillager",
        "TombSummon",
        "StatueSpawn",
        "SmiteOrb",
        "CardinalAide",
        "GateBoss",
        "DraculaMinion",
        "Summon",
        "Minion",
        "Chieftain",
        "ConstrainingPole",
        "Horse",
        "EnchantedCross",
        "DivineAngel",
        "FallenAngel"
    ];
    public static IEnumerable<Entity> GetEntitiesEnumerable(EntityQuery entityQuery, bool filterTargets = false) // not sure if need to actually check for empty buff buffer for quest targets but don't really want to find out
    {
        JobHandle handle = GetEntities(entityQuery, out NativeArray<Entity> entities, Allocator.TempJob);
        handle.Complete();
        try
        {
            foreach (Entity entity in entities)
            {
                if (filterTargets && entity.TryGetComponent(out PrefabGUID unitPrefab))
                {
                    string prefabName = unitPrefab.LookupName();
                    if (!FilteredTargetNames.Contains(prefabName)) yield return entity;
                }
                else if (EntityManager.Exists(entity))
                {
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
}
