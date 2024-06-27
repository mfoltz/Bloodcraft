using Bloodcraft.Systems.Experience;
using ProjectM;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
/*
namespace Bloodcraft.Services;
internal partial class DamageEventSystem(IntPtr pointer) : SystemBase(pointer)
{
    public NativeArray<DealDamageEvent> DealDamageSystemEvents;
    IntPtr JobPtr;

    // Custom job class inheriting from IJobParallelFor, definitely know what I'm doing here
    class DealDamageJob(IntPtr pointer) : IJob(pointer)
    {
        public NativeArray<DealDamageEvent> DealDamageEvents;
        public override void Execute()
        {
            foreach (DealDamageEvent dealDamageEvent in DealDamageEvents)
            {
                Execute(in dealDamageEvent); // Call the static method
            }
        }
        public static void Execute(in DealDamageEvent dealDamageEvent)
        {
            // Perform operations on the component
            if (dealDamageEvent.SpellSource.TryGetComponent(out EntityOwner entityOwner) &&
                entityOwner.Owner.TryGetComponent(out PlayerCharacter source))
            {
                ulong steamId = source.UserEntity.Read<User>().PlatformId;
                if (Core.DataStructures.PlayerClasses.TryGetValue(steamId, out var playerClasses) &&
                    playerClasses.Keys.Count != 0)
                {
                    LevelingSystem.PlayerClasses playerClass = playerClasses.Keys.First();
                    Core.Log.LogInfo($"Player class: {playerClass} | Player Name: {source.Name.Value}");
                }
            }
        }
    }
    public override void OnCreate()
    {
        // OnCreate
        //IJobExtensions.EarlyJobInit<DealDamageJob(IntPtr.Zero)>();
        //JobPtr = IJobExtensions.GetReflectionData<DealDamageJob>();
    }
    public override void OnDestroy()
    {
        // OnDestroy
    }
    public override void OnUpdate()
    {
        // Retrieve the deal damage events, check array length
        NativeArray<DealDamageEvent> dealDamageEvents = DealDamageSystemEvents;

        if (!DealDamageSystemEvents.IsCreated || DealDamageSystemEvents.Length == 0)
        {
            return;
        }
        
        // Create an instance of the custom job handler
        DealDamageJob job = new(JobPtr)
        {
            DealDamageEvents = dealDamageEvents
        };
        // Execute the job
        JobHandle jobHandle = IJobExtensions.Schedule(job);
        jobHandle.Complete();

        // Dispose the native array
        dealDamageEvents.Dispose();   
    }
}
*/
