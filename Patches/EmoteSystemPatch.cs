using Bloodcraft.Systems.Familiars;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using User = ProjectM.Network.User;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal class EmoteSystemPatch
{
    public static readonly Dictionary<PrefabGUID, Action<Entity, Entity, ulong>> actions = new()
    {
        { new(1177797340), CallDismiss }, // Wave
    };

    [HarmonyPatch(typeof(EmoteSystem), nameof(EmoteSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(EmoteSystem __instance)
    {
        NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.TempJob);
        try
        {
            foreach (var entity in entities)
            {
                UseEmoteEvent useEmoteEvent = entity.Read<UseEmoteEvent>();
                FromCharacter fromCharacter = entity.Read<FromCharacter>();

                Entity userEntity = fromCharacter.User;
                Entity character = fromCharacter.Character;

                ulong steamId = userEntity.Read<User>().PlatformId;

                if (Core.DataStructures.PlayerBools.TryGetValue(steamId, out var bools) && bools["Emotes"])
                {
                    if (actions.TryGetValue(useEmoteEvent.Action, out var action)) action.Invoke(userEntity, character, steamId);
                }
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogInfo(ex.Message);
        }
        finally
        {
            entities.Dispose();
        }
    }

    public static void CallDismiss(Entity userEntity, Entity character, ulong playerId)
    {
        EntityManager entityManager = Core.EntityManager;
        if (Core.DataStructures.FamiliarActives.TryGetValue(playerId, out var data) && !data.Item2.Equals(0)) // 0 means no active familiar
        {
            Entity familiar = FamiliarSummonSystem.FamiliarUtilities.FindPlayerFamiliar(character); // return following entity matching guidhash in FamiliarActives
            
            if (!data.Item1.Equals(Entity.Null) && Core.EntityManager.Exists(data.Item1))
            {
                familiar = data.Item1;
            }

            if (familiar == Entity.Null)
            {
                ServerChatUtils.SendSystemMessageToClient(entityManager, userEntity.Read<User>(), "No active familiar found to enable/disable.");
                return;
            }

            if (!familiar.Has<Disabled>())
            {
                entityManager.AddComponent<Disabled>(familiar);

                Follower follower = familiar.Read<Follower>();
                follower.Followed._Value = Entity.Null;
                familiar.Write(follower);
                data = (familiar, data.Item2);
                Core.DataStructures.FamiliarActives[playerId] = data;
                Core.DataStructures.SavePlayerFamiliarActives();
             
                ServerChatUtils.SendSystemMessageToClient(entityManager, userEntity.Read<User>(), "Familiar <color=red>disabled</color>.");
            }
            else if (familiar.Has<Disabled>())
            {
                familiar.Remove<Disabled>();

                Follower follower = familiar.Read<Follower>();
                follower.Followed._Value = character;
                familiar.Write(follower);

                familiar.Write(new Translation { Value = character.Read<LocalToWorld>().Position });

                data = (Entity.Null, data.Item2);
                Core.DataStructures.FamiliarActives[playerId] = data;
                Core.DataStructures.SavePlayerFamiliarActives();
                ServerChatUtils.SendSystemMessageToClient(entityManager, userEntity.Read<User>(), "Familiar <color=green>enabled</color>.");
            }

        }
        else
        {
            ServerChatUtils.SendSystemMessageToClient(entityManager, userEntity.Read<User>(), "No active familiar to enable/disable.");
        }
    } 
    
}