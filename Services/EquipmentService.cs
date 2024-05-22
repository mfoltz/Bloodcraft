using Il2CppInterop.Runtime;
using ProjectM;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Services
{
    internal class EquipmentService
    {
        public readonly Dictionary<PrefabGUID, float> ArmorLevelSources = [];
        public readonly Dictionary<PrefabGUID, float> WeaponLevelSources = [];
        public readonly Dictionary<PrefabGUID, float> SpellLevelSources = [];
        public EquipmentService()
        {
            PrefabCollectionSystem prefabCollectionSystem = Core.PrefabCollectionSystem;

            EntityQueryDesc queryDesc = new()
            {
                All = new ComponentType[] { new(Il2CppType.Of<Equippable>(), ComponentType.AccessMode.ReadOnly) },
                Options = EntityQueryOptions.IncludeAll
            };
            EntityQuery query = Core.EntityManager.CreateEntityQuery(queryDesc);

            // Iterate through each entity in the query
            foreach (Entity source in query.ToEntityArray(Allocator.Temp))
            {
                PrefabGUID prefabGUID = source.Read<PrefabGUID>();
                Entity baseEntity = prefabCollectionSystem._PrefabGuidToEntityMap[prefabGUID];
                int entityIndex = baseEntity.Index;
                if (Plugin.LevelingSystem.Value && baseEntity.Has<ArmorLevelSource>())
                {
                    float level = baseEntity.Read<ArmorLevelSource>().Level;
                    if (!ArmorLevelSources.ContainsKey(prefabGUID))
                    {
                        ArmorLevelSources.Add(prefabGUID, level);
                    }
                    baseEntity.Write(new ArmorLevelSource { Level = 0 });
                }
                else if (Plugin.LevelingSystem.Value && baseEntity.Has<SpellLevelSource>())
                {
                    float level = baseEntity.Read<SpellLevelSource>().Level;
                    if (!SpellLevelSources.ContainsKey(prefabGUID))
                    {
                        SpellLevelSources.Add(prefabGUID, level);
                    }
                    baseEntity.Write(new SpellLevelSource { Level = 0 });
                }
                else if (Plugin.ExpertiseSystem.Value && baseEntity.Has<WeaponLevelSource>())
                {
                    float level = baseEntity.Read<WeaponLevelSource>().Level;
                    if (!WeaponLevelSources.ContainsKey(prefabGUID))
                    {
                        WeaponLevelSources.Add(prefabGUID, level);
                    }
                    baseEntity.Write(new WeaponLevelSource { Level = 0 });
                }
            }
        }
    }
}
