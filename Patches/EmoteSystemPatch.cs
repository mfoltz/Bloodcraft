using Bloodcraft.Systems.Familiars;
using Bloodcraft.Services;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using User = ProjectM.Network.User;
using static ProjectM.VoiceMapping;

namespace Bloodcraft.Patches;

[HarmonyPatch]
public static class EmoteSystemPatch
{
    /*
    public static class Coordinator // this is for coordination on emote usage with KindredAuras (#soon) and ChatGPT insists that static same-named classes can be used for this purpose without making either mod dependent on the other. we'll see about that
    {
        public static Dictionary<ulong, bool> FamiliarEmotes { get; set; } = [];
        public static Dictionary<ulong, bool> AuraEmotes { get; set; } = [];
    }
    */
    static readonly PrefabGUID dominateBuff = new(-1447419822);
    static readonly PrefabGUID invulnerableBuff = new(-480024072);
    static readonly PrefabGUID ignoredFaction = new(-1430861195);
    static readonly PrefabGUID playerFaction = new(1106458752);
    static readonly PrefabGUID combatBuff = new(581443919);
    static readonly PrefabGUID pvpCombatBuff = new(697095869);

    public static readonly Dictionary<PrefabGUID, Action<Entity, Entity, ulong>> actions = new()
    {
        { new(1177797340), CallDismiss }, // Wave
        { new(-370061286), CombatMode }, // Salute
        { new(-26826346), BindPreset } // clap
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
                if (!Core.hasInitialized) continue;

                UseEmoteEvent useEmoteEvent = entity.Read<UseEmoteEvent>();
                FromCharacter fromCharacter = entity.Read<FromCharacter>();

                Entity userEntity = fromCharacter.User;
                Entity character = fromCharacter.Character;

                ulong steamId = userEntity.Read<User>().PlatformId;
                /*
                if (Core.DataStructures.PlayerBools.TryGetValue(steamId, out var bools) && bools["Emotes"] && (!Coordinator.AuraEmotes.ContainsKey(steamId) || !Coordinator.AuraEmotes[steamId]))
                {
                    if (actions.TryGetValue(useEmoteEvent.Action, out var action)) action.Invoke(userEntity, character, steamId);
                }
                */
                if (Core.DataStructures.PlayerBools.TryGetValue(steamId, out var bools) && bools["Emotes"])
                {
                    if (actions.TryGetValue(useEmoteEvent.Action, out var action) && !Core.ServerGameManager.TryGetBuff(character, dominateBuff.ToIdentifier(), out Entity _)) action.Invoke(userEntity, character, steamId);
                    else if (Core.ServerGameManager.TryGetBuff(character, dominateBuff.ToIdentifier(), out Entity _))
                    {
                        LocalizationService.HandleServerReply(Core.EntityManager, userEntity.Read<User>(), "You can't call a familiar while dominating presence is active.");
                    }
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
    public static void BindPreset(Entity userEntity, Entity character, ulong steamId)
    {
        EntityManager entityManager = Core.EntityManager;

        if (!Core.DataStructures.FamiliarChoice.ContainsKey(steamId))
        {
            LocalizationService.HandleServerReply(entityManager, userEntity.Read<User>(), "No familiar preset found to bind, use .fam bind # at least once first.");
            return;
        }

        int preset = Core.DataStructures.FamiliarChoice[steamId];
        Entity familiar = FamiliarSummonUtilities.FamiliarUtilities.FindPlayerFamiliar(character);
        User user = userEntity.Read<User>();

        if (Core.ServerGameManager.TryGetBuff(character, combatBuff.ToIdentifier(), out Entity _) || Core.ServerGameManager.TryGetBuff(character, pvpCombatBuff.ToIdentifier(), out Entity _) || Core.ServerGameManager.TryGetBuff(character, dominateBuff.ToIdentifier(), out Entity _))
        {
            LocalizationService.HandleServerReply(entityManager, user, "You can't bind a familiar while in combat or dominating presence is active.");
            return;
        }

        if (familiar != Entity.Null)
        {
            LocalizationService.HandleServerReply(entityManager, user, "You already have an active familiar.");
            return;
        }

        string set = Core.DataStructures.FamiliarSet[steamId];

        if (string.IsNullOrEmpty(set))
        {
            LocalizationService.HandleServerReply(entityManager, user, "You don't have a box selected. Use .fam boxes to see available boxes then choose one with .fam cb [BoxName]");
            return;
        }

        if (Core.DataStructures.FamiliarActives.TryGetValue(steamId, out var data) && data.Familiar.Equals(Entity.Null) && data.FamKey.Equals(0) && Core.FamiliarUnlocksManager.LoadUnlockedFamiliars(steamId).UnlockedFamiliars.TryGetValue(set, out var famKeys))
        {
            Core.DataStructures.PlayerBools[steamId]["Binding"] = true;
            if (preset < 1 || preset > famKeys.Count)
            {
                LocalizationService.HandleServerReply(entityManager, user, $"Invalid choice, please use 1 to {famKeys.Count} (Current List:<color=white>{set}</color>) and make sure to update preset for new active boxes.");
                return;
            }
            if (!Core.DataStructures.FamiliarChoice.ContainsKey(steamId)) // cache, set choice once per session then can use emote to bind same choice
            {
                Core.DataStructures.FamiliarChoice[steamId] = preset;
            }
            data = new(Entity.Null, famKeys[preset - 1]);
            Core.DataStructures.FamiliarActives[steamId] = data;
            Core.DataStructures.SavePlayerFamiliarActives();
            FamiliarSummonUtilities.SummonFamiliar(character, userEntity, famKeys[preset - 1]);
            //character.Add<AlertAllies>();

        }
        else
        {
            LocalizationService.HandleServerReply(entityManager, user, "Couldn't find familiar or familiar already active.");
        }
    }

    public static void CallDismiss(Entity userEntity, Entity character, ulong playerId)
    {
        EntityManager entityManager = Core.EntityManager;

        if (Core.DataStructures.FamiliarActives.TryGetValue(playerId, out var data) && !data.FamKey.Equals(0)) // 0 means no active familiar
        {
            Entity familiar = FamiliarSummonUtilities.FamiliarUtilities.FindPlayerFamiliar(character); // return following entity matching Guidhash in FamiliarActives
            
            if (!data.Familiar.Equals(Entity.Null) && Core.EntityManager.Exists(data.Familiar))
            {
                familiar = data.Familiar;
            }

            if (familiar == Entity.Null)
            {
                LocalizationService.HandleServerReply(entityManager, userEntity.Read<User>(), "No active familiar found to enable or disable.");
                return;
            }

            if (!familiar.Has<Disabled>())
            {
                if (FamiliarPatches.familiarMinions.ContainsKey(data.Familiar)) Core.FamiliarService.HandleFamiliarMinions(familiar);
                entityManager.AddComponent<Disabled>(familiar);
                
                Follower follower = familiar.Read<Follower>();
                follower.Followed._Value = Entity.Null;
                familiar.Write(follower);

                var buffer = character.ReadBuffer<FollowerBuffer>();
                for (int i = 0; i < buffer.Length; i++)
                {
                    if (buffer[i].Entity._Entity.Equals(familiar))
                    {
                        buffer.RemoveAt(i);
                        break;
                    }
                }

                data = (familiar, data.FamKey);
                Core.DataStructures.FamiliarActives[playerId] = data;
                Core.DataStructures.SavePlayerFamiliarActives();

                LocalizationService.HandleServerReply(entityManager, userEntity.Read<User>(), "Familiar <color=red>disabled</color>.");
            }
            else if (familiar.Has<Disabled>())
            {
                familiar.Write(new Translation { Value = character.Read<LocalToWorld>().Position });
                familiar.Remove<Disabled>();

                
                Follower follower = familiar.Read<Follower>();
                follower.Followed._Value = character;
                familiar.Write(follower);
                
                data = (Entity.Null, data.FamKey);
                Core.DataStructures.FamiliarActives[playerId] = data;
                Core.DataStructures.SavePlayerFamiliarActives();
                LocalizationService.HandleServerReply(entityManager, userEntity.Read<User>(), "Familiar <color=green>enabled</color>.");
            }
        }
        else
        {
            LocalizationService.HandleServerReply(entityManager, userEntity.Read<User>(), "No active familiar to enable or disable.");
        }
    }
    public static void CombatMode(Entity userEntity, Entity character, ulong playerId)
    {
        EntityManager entityManager = Core.EntityManager;

        if (!Plugin.FamiliarCombat.Value)
        {
            LocalizationService.HandleServerReply(entityManager, userEntity.Read<User>(), "Familiar combat is not enabled.");
            return;
        }

        ServerGameManager serverGameManager = Core.ServerGameManager;
        if (Core.DataStructures.FamiliarActives.TryGetValue(playerId, out var data) && !data.FamKey.Equals(0)) // 0 means no active familiar
        {
            Entity familiar = FamiliarSummonUtilities.FamiliarUtilities.FindPlayerFamiliar(character); // return following entity matching Guidhash in FamiliarActives

            if (!data.Familiar.Equals(Entity.Null) && Core.EntityManager.Exists(data.Familiar))
            {
                familiar = data.Familiar;
            }

            if (familiar == Entity.Null)
            {
                LocalizationService.HandleServerReply(entityManager, userEntity.Read<User>(), "No active familiar found to enable/disable combat mode for.");
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
                
                LocalizationService.HandleServerReply(entityManager, userEntity.Read<User>(), "Familiar combat <color=green>enabled</color>.");
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
                }
                LocalizationService.HandleServerReply(entityManager, userEntity.Read<User>(), "Familiar combat <color=red>disabled</color>.");
            }
        }
        else
        {
            LocalizationService.HandleServerReply(entityManager, userEntity.Read<User>(), "No active familiar found to enable or disable combat mode for.");
        }
    }
}