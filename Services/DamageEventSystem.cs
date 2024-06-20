using Bloodcraft.Systems.Experience;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
/* // nothing to see here
namespace Bloodcraft.Services;
public partial class DamageEventSystem(IntPtr pointer) : SystemBase(pointer)
{
    EntityQuery damageEventQuery;
    // Custom job class inheriting from IJob, definitely know what I'm doing here
    public class DealDamageJob(IntPtr pointer) : IJob(pointer)
    {
        public NativeArray<DealDamageEvent> DealDamageEvents;
        public override void Execute()
        {
            foreach(DealDamageEvent dealDamageEvent in DealDamageEvents)
            {
                Core.Log.LogInfo("DealDamageJob OnUpdate executing...");
                Execute(in dealDamageEvent);
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
        damageEventQuery = Core.Server.GetExistingSystemManaged<DealDamageSystem>()._Query;   
    }
    public override void OnUpdate()
    {
        // Retrieve the deal damage events
        NativeArray<DealDamageEvent> dealDamageEvents = damageEventQuery.ToComponentDataArray<DealDamageEvent>(Allocator.TempJob);

        // Create an instance of the custom job handler
        IntPtr jobPtr = IJobExtensions.GetReflectionData<IJob>();
        var job = new DealDamageJob(jobPtr)
        {
            DealDamageEvents = dealDamageEvents
        };

        // Execute the job
        job.Execute();
        
        // Dispose the native array
        dealDamageEvents.Dispose();
    }
}
*/

