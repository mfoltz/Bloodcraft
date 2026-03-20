//using Bloodcraft.Utilities;
//using ProjectM;
//using ProjectM.Network;
//using Unity.Entities;
//using IJobChunk = Bloodcraft.Utilities.ChunkJobs.IJobChunk;

//namespace Bloodcraft.Systems;

//public sealed class DeathContainerSystem : SystemBase
//{
//    internal static DeathContainerSystem Instance { get; set; }
//    EndSimulationEntityCommandBufferSystem EntityCommandBuffer { get; } = Instance.World.GetExistingSystemManaged<EndSimulationEntityCommandBufferSystem>();

//    EntityQuery _mapIconQuery;
//    EntityTypeHandle _entityHandle;

//    // set user-related info as needed**
//    ComponentTypeHandle<MapIconData> _mapIconDataHandle; // MapIconData.TargetUser = userEntity;
//    ComponentTypeHandle<MapIconTargetEntity> _mapIconTargetEntityHandle; // MapIconTargetEntity.TargetEntity -> death container drop entity

//    BufferTypeHandle<SyncToUserBuffer> _syncToUserHandle; // add user to buffer;
//    BufferLookup<InventoryInstanceElement> _inventoryInstanceElementLookup; // InventoryInstanceElement.ExternalInventoryEntity -> death container external inventory, then that entity's owner and/or team**

//    public override void OnCreate()
//    {
//        Instance = this;
//        Enabled = true;
//        OnCreateInternal();
//    }

//    void OnCreateInternal()
//    {
//        _entityHandle = GetEntityTypeHandle();
//        _mapIconQuery = GetEntityQuery(new EntityQueryDesc
//        {
//            All = new ComponentType[] { Il2CppTypeOf<MapIconData>(), Il2CppTypeOf<MapIconTargetEntity>(), Il2CppTypeOf<SyncToUserBuffer>() },
//            Options = EntityQueryOptions.IncludeDisabled
//        });
//        RequireAnyForUpdate(_mapIconQuery);
//        UpdateHandles();
//        UpdateLookups();
//    }

//    void OnBeforeUpdate()
//    {
//        UpdateHandles();
//        UpdateLookups();
//    }

//    public override void OnUpdate()
//    {
//        OnBeforeUpdate();
//        SyncUserJob syncUserJob = new()
//        {
//            EntityCommandBuffer = GetCommandBuffer(),
//            EntityHandle = _entityHandle,
//            MapIconDataHandle = _mapIconDataHandle,
//            MapIconTargetEntityHandle = _mapIconTargetEntityHandle,
//            SyncToUserHandle = _syncToUserHandle,
//            InventoryInstanceElementLookup = _inventoryInstanceElementLookup
//        };
//        _mapIconQuery.ForEach(ref syncUserJob);
//    }

//    void UpdateHandles()
//    {
//        _entityHandle.Update(this);
//        _mapIconDataHandle.ResolveHandle(this);
//        _mapIconTargetEntityHandle.ResolveHandle(this);
//        _syncToUserHandle.ResolveHandle(this);
//    }

//    void UpdateLookups()
//    {
//        _inventoryInstanceElementLookup.ResolveLookup(this);
//    }

//    EntityCommandBuffer GetCommandBuffer()
//    {
//        return EntityCommandBuffer.CreateCommandBuffer();
//    }

//    public struct SyncUserJob : IJobChunk // write jobs, work backwards**
//    {
//        public EntityCommandBuffer EntityCommandBuffer;
//        public EntityTypeHandle EntityHandle;

//        public BufferTypeHandle<SyncToUserBuffer> SyncToUserHandle;
//        public BufferLookup<InventoryInstanceElement> InventoryInstanceElementLookup;

//        public ComponentTypeHandle<MapIconData> MapIconDataHandle;
//        public ComponentTypeHandle<MapIconTargetEntity> MapIconTargetEntityHandle;
//        public void Execute(ref ArchetypeChunk chunk)
//        {
//            for (int i = 0; i < chunk.Count; ++i)
//            {
//                // find root player owner from death bag
//            }
//        }
//    }
//}
