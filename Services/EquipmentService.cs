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
                Options = EntityQueryOptions.IncludeDisabled
            };
            EntityQuery query = Core.EntityManager.CreateEntityQuery(queryDesc);
            foreach (Entity source in query.ToEntityArray(Allocator.Temp))
            {
                PrefabGUID prefabGUID = source.Read<PrefabGUID>();
                Entity baseEntity = prefabCollectionSystem._PrefabGuidToEntityMap[prefabGUID];

                if (Plugin.LevelingSystem.Value && baseEntity.Has<ArmorLevelSource>())
                {
                    if (!ArmorLevelSources.ContainsKey(prefabGUID)) ArmorLevelSources.Add(prefabGUID, baseEntity.Read<ArmorLevelSource>().Level);
                    baseEntity.Write(new ArmorLevelSource { Level = 0 });
                }
                else if (Plugin.LevelingSystem.Value && baseEntity.Has<SpellLevelSource>())
                {
                    if (!SpellLevelSources.ContainsKey(prefabGUID)) SpellLevelSources.Add(prefabGUID, baseEntity.Read<SpellLevelSource>().Level);
                    baseEntity.Write(new SpellLevelSource { Level = 0 });
                }
                else if (Plugin.ExpertiseSystem.Value && baseEntity.Has<WeaponLevelSource>())
                {
                    if (!WeaponLevelSources.ContainsKey(prefabGUID)) WeaponLevelSources.Add(prefabGUID, baseEntity.Read<WeaponLevelSource>().Level);
                    baseEntity.Write(new WeaponLevelSource { Level = 0 });
                }
            }
        }
    }
}
