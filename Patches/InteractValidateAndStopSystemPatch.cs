using Bloodcraft.Services;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class InteractValidateAndStopSystemPatch
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    static readonly PrefabGUID dominateBuff = new(-1447419822);
    static readonly PrefabGUID pickupResource = new(165220777);

    [HarmonyPatch(typeof(InteractValidateAndStopSystemServer), nameof(InteractValidateAndStopSystemServer.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(InteractValidateAndStopSystemServer __instance)
    {
        if (!Core.hasInitialized) return;
        if (!ConfigService.FamiliarSystem) return;

        NativeArray<Entity> entities = __instance.__query_195794971_3.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.Has<EntityOwner>()) continue;
                if (!entity.TryGetComponent(out PrefabGUID prefabGUID)) continue;
                
                if (entity.GetOwner().TryGetPlayer(out Entity player) && !ServerGameManager.HasBuff(player, dominateBuff.ToIdentifier()))
                {
                    if (prefabGUID.GuidHash.Equals(-986064531) || prefabGUID.GuidHash.Equals(985937733)) // player using world or castle waygate
                    {                        
                        Entity familiar = FamiliarUtilities.FindPlayerFamiliar(player);
                        if (!familiar.Exists()) continue;

                        Entity userEntity = player.Read<PlayerCharacter>().UserEntity;
                        ulong steamID = userEntity.Read<User>().PlatformId;

                        if (!familiar.Has<Disabled>()) // if not disabled, dismiss to be able to use waygates
                        {
                            EmoteSystemPatch.CallDismiss(userEntity, player, steamID); // auto dismiss familiar 
                        }
                        else if (familiar.Has<Disabled>()) // if disabled, call after using waygate to summon on arrival
                        {
                            EmoteSystemPatch.CallDismiss(userEntity, player, steamID); // auto dismiss familiar 
                        }
                    }
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
}
