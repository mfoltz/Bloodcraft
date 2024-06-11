using Bloodcraft.Systems.Familiars;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using User = ProjectM.Network.User;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal class EmoteSystemPatch
{
    static readonly PrefabGUID invulnerableBuff = new(-480024072);
    static readonly PrefabGUID ignoredFaction = new(-1430861195);
    static readonly PrefabGUID playerFaction = new(1106458752);

    public static readonly Dictionary<PrefabGUID, Action<Entity, Entity, ulong>> actions = new()
    {
        { new(1177797340), CallDismiss }, // Wave
        { new(-370061286), CombatMode } // Salute
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
            Entity familiar = FamiliarSummonSystem.FamiliarUtilities.FindPlayerFamiliar(character); // return following entity matching Guidhash in FamiliarActives
            
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
                familiar.Write(new Translation { Value = character.Read<LocalToWorld>().Position });
                familiar.Remove<Disabled>();

                
                Follower follower = familiar.Read<Follower>();
                follower.Followed._Value = character;
                familiar.Write(follower);
                
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
    public static void CombatMode(Entity userEntity, Entity character, ulong playerId)
    {
        ServerGameManager serverGameManager = Core.ServerGameManager;
        EntityManager entityManager = Core.EntityManager;
        if (Core.DataStructures.FamiliarActives.TryGetValue(playerId, out var data) && !data.Item2.Equals(0)) // 0 means no active familiar
        {
            Entity familiar = FamiliarSummonSystem.FamiliarUtilities.FindPlayerFamiliar(character); // return following entity matching Guidhash in FamiliarActives

            if (!data.Item1.Equals(Entity.Null) && Core.EntityManager.Exists(data.Item1))
            {
                familiar = data.Item1;
            }

            if (familiar == Entity.Null)
            {
                ServerChatUtils.SendSystemMessageToClient(entityManager, userEntity.Read<User>(), "No active familiar found to enable/disable combat mode for.");
                return;
            }

            if (serverGameManager.TryGetBuff(familiar, invulnerableBuff.ToIdentifier(), out Entity _)) // remove and enable combat
            {
                
                BuffUtility.BuffSpawner buffSpawner = BuffUtility.BuffSpawner.Create(serverGameManager);
                EntityCommandBuffer entityCommandBuffer = Core.EntityCommandBufferSystem.CreateCommandBuffer();
                BuffUtility.TryRemoveBuff(ref buffSpawner, entityCommandBuffer, invulnerableBuff, familiar);

                FactionReference factionReference = familiar.Read<FactionReference>();
                factionReference.FactionGuid._Value = playerFaction;
                familiar.Write(factionReference);

                AggroConsumer aggroConsumer = familiar.Read<AggroConsumer>();
                aggroConsumer.Active._Value = true;
                familiar.Write(aggroConsumer);

                Aggroable aggroable = familiar.Read<Aggroable>();
                aggroable.Value._Value = true;
                aggroable.DistanceFactor._Value = 1f;
                aggroable.AggroFactor._Value = 1f;
                familiar.Write(aggroable);
                
                ServerChatUtils.SendSystemMessageToClient(entityManager, userEntity.Read<User>(), "Familiar combat <color=green>enabled</color>.");
            }
            else // if not, disable combat
            {  
                FactionReference factionReference = familiar.Read<FactionReference>();
                factionReference.FactionGuid._Value = ignoredFaction;
                familiar.Write(factionReference);

                AggroConsumer aggroConsumer = familiar.Read<AggroConsumer>();
                aggroConsumer.Active._Value = false;
                familiar.Write(aggroConsumer);

                Aggroable aggroable = familiar.Read<Aggroable>();
                aggroable.Value._Value = false;
                aggroable.DistanceFactor._Value = 0f;
                aggroable.AggroFactor._Value = 0f;
                familiar.Write(aggroable);

                DebugEventsSystem debugEventsSystem = Core.DebugEventsSystem;
                ApplyBuffDebugEvent applyBuffDebugEvent = new()
                {
                    BuffPrefabGUID = invulnerableBuff,
                };
                FromCharacter fromCharacter = new()
                {
                    Character = familiar,
                    User = userEntity,
                };
                debugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
                if (serverGameManager.TryGetBuff(familiar, invulnerableBuff.ToIdentifier(), out Entity invlunerableBuff))
                {
                    if (invlunerableBuff.Has<LifeTime>())
                    {
                        var lifetime = invlunerableBuff.Read<LifeTime>();
                        lifetime.Duration = -1;
                        lifetime.EndAction = LifeTimeEndAction.None;
                        invlunerableBuff.Write(lifetime);
                    }
                    else
                    {
                        LifeTime lifeTime = new()
                        {
                            Duration = -1,
                            EndAction = LifeTimeEndAction.None
                        };
                        invlunerableBuff.Add<LifeTime>();
                        invlunerableBuff.Write(lifeTime);
                    }
                    
                }
                ServerChatUtils.SendSystemMessageToClient(entityManager, userEntity.Read<User>(), "Familiar combat <color=red>disabled</color>.");
            }
        }
        else
        {
            ServerChatUtils.SendSystemMessageToClient(entityManager, userEntity.Read<User>(), "No active familiar found to enable/disable combat mode for.");
        }
    }
}