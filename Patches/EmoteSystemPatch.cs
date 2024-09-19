using Bloodcraft.Services;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using static Bloodcraft.Patches.LinkMinionToOwnerOnSpawnSystemPatch;
using static Bloodcraft.Services.DataService.FamiliarPersistence;
using User = ProjectM.Network.User;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class EmoteSystemPatch
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static DebugEventsSystem DebugEventsSystem => SystemService.DebugEventsSystem;
    static EntityCommandBufferSystem EntityCommandBufferSystem => SystemService.EntityCommandBufferSystem;

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
        if (!Core.hasInitialized) return;
        if (!ConfigService.FamiliarSystem) return;

        NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (var entity in entities)
            {
                UseEmoteEvent useEmoteEvent = entity.Read<UseEmoteEvent>();
                FromCharacter fromCharacter = entity.Read<FromCharacter>();

                Entity userEntity = fromCharacter.User;
                Entity character = fromCharacter.Character;
                ulong steamId = userEntity.Read<User>().PlatformId;

                if (PlayerUtilities.GetPlayerBool(steamId, "Emotes"))
                {
                    if (actions.TryGetValue(useEmoteEvent.Action, out var action) && !ServerGameManager.TryGetBuff(character, dominateBuff.ToIdentifier(), out Entity _)) action.Invoke(userEntity, character, steamId);
                    else if (ServerGameManager.HasBuff(character, dominateBuff.ToIdentifier()))
                    {
                        LocalizationService.HandleServerReply(EntityManager, userEntity.Read<User>(), "You can't call a familiar while dominating presence is active.");
                    }
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
    public static void BindPreset(Entity userEntity, Entity character, ulong steamId)
    {
        if (!steamId.TryGetFamiliarDefault(out var preset))
        {
            LocalizationService.HandleServerReply(EntityManager, userEntity.Read<User>(), "No familiar preset found to bind, use .fam bind # at least once first.");
            return;
        }

        Entity familiar = FamiliarUtilities.FindPlayerFamiliar(character);
        User user = userEntity.Read<User>();

        if (ServerGameManager.HasBuff(character, combatBuff.ToIdentifier()) || ServerGameManager.HasBuff(character, pvpCombatBuff.ToIdentifier()) || ServerGameManager.HasBuff(character, dominateBuff.ToIdentifier()))
        {
            LocalizationService.HandleServerReply(EntityManager, user, "You can't bind a familiar while in combat or dominating presence is active.");
            return;
        }

        if (familiar.Exists())
        {
            LocalizationService.HandleServerReply(EntityManager, user, "You already have an active familiar.");
            return;
        }

        string set = steamId.TryGetFamiliarBox(out set) ? set : "";

        if (string.IsNullOrEmpty(set))
        {
            LocalizationService.HandleServerReply(EntityManager, user, "You don't have a box selected. Use .fam boxes to see available boxes then choose one with .fam cb [BoxName]");
            return;
        }
        
        if (steamId.TryGetFamiliarActives(out var data) && !data.Familiar.Exists() && data.FamKey.Equals(0) && FamiliarUnlocksManager.LoadUnlockedFamiliars(steamId).UnlockedFamiliars.TryGetValue(set, out var famKeys))
        {
            PlayerUtilities.SetPlayerBool(steamId, "Binding", true);

            if (preset < 1 || preset > famKeys.Count)
            {
                LocalizationService.HandleServerReply(EntityManager, user, $"Invalid choice, please use 1 to {famKeys.Count} (Current List:<color=white>{set}</color>) and make sure to update preset for new active boxes.");
                return;
            }

            data = new(Entity.Null, famKeys[preset - 1]);
            steamId.SetFamiliarActives(data);

            FamiliarSummonSystem.SummonFamiliar(character, userEntity, famKeys[preset - 1]);
        }
        else
        {
            LocalizationService.HandleServerReply(EntityManager, user, "Couldn't find familiar or familiar already active.");
        }
    }
    public static void CallDismiss(Entity userEntity, Entity character, ulong playerId)
    {
        if (playerId.TryGetFamiliarActives(out var data) && !data.FamKey.Equals(0)) // 0 means no active familiar
        {
            Entity familiar = FamiliarUtilities.FindPlayerFamiliar(character); // return following entity matching Guidhash in FamiliarActives

            if (!data.Familiar.Equals(Entity.Null) && EntityManager.Exists(data.Familiar))
            {
                familiar = data.Familiar;
            }

            if (familiar == Entity.Null)
            {
                LocalizationService.HandleServerReply(EntityManager, userEntity.Read<User>(), "No active familiar found to enable or disable.");
                return;
            }

            if (!familiar.Has<Disabled>())
            {
                if (FamiliarMinions.ContainsKey(data.Familiar)) FamiliarUtilities.HandleFamiliarMinions(familiar);
                EntityManager.AddComponent<Disabled>(familiar);

                Follower follower = familiar.Read<Follower>();
                follower.Followed._Value = Entity.Null;
                familiar.Write(follower);

                AggroConsumer aggroConsumer = familiar.Read<AggroConsumer>();
                aggroConsumer.Active._Value = false;
                aggroConsumer.AggroTarget._Entity = Entity.Null;
                aggroConsumer.AlertTarget._Entity = Entity.Null;
                familiar.Write(aggroConsumer);

                var buffer = character.ReadBuffer<FollowerBuffer>();
                for (int i = 0; i < buffer.Length; i++)
                {
                    if (buffer[i].Entity._Entity.Equals(familiar))
                    {
                        buffer.RemoveAt(i);
                        break;
                    }
                }

                data = (familiar, data.FamKey); // entity stored when dismissed
                playerId.SetFamiliarActives(data);

                LocalizationService.HandleServerReply(EntityManager, userEntity.Read<User>(), "Familiar <color=red>disabled</color>.");
            }
            else if (familiar.Has<Disabled>())
            {
                float3 position = character.Read<Translation>().Value;
                familiar.Remove<Disabled>();

                familiar.Write(new Translation { Value = position });
                familiar.Write(new LastTranslation { Value = position });

                Follower follower = familiar.Read<Follower>();
                follower.Followed._Value = character;
                familiar.Write(follower);


                if (ConfigService.FamiliarCombat)
                {
                    AggroConsumer aggroConsumer = familiar.Read<AggroConsumer>();
                    aggroConsumer.Active._Value = true;
                    familiar.Write(aggroConsumer);
                }

                data = (Entity.Null, data.FamKey);
                playerId.SetFamiliarActives(data);

                LocalizationService.HandleServerReply(EntityManager, userEntity.Read<User>(), "Familiar <color=green>enabled</color>.");
            }
        }
        else
        {
            LocalizationService.HandleServerReply(EntityManager, userEntity.Read<User>(), "No active familiar to enable or disable.");
        }
    }
    public static void CombatMode(Entity userEntity, Entity character, ulong playerId)
    {
        if (!ConfigService.FamiliarCombat)
        {
            LocalizationService.HandleServerReply(EntityManager, userEntity.Read<User>(), "Familiar combat is not enabled.");
            return;
        }

        if (playerId.TryGetFamiliarActives(out var data) && !data.FamKey.Equals(0)) // 0 means no active familiar
        {
            Entity familiar = FamiliarUtilities.FindPlayerFamiliar(character); // return following entity matching Guidhash in FamiliarActives

            if (!familiar.Exists() && data.Familiar.Exists())
            {
                familiar = data.Familiar;
            }

            if (familiar == Entity.Null)
            {
                LocalizationService.HandleServerReply(EntityManager, userEntity.Read<User>(), "No active familiar found to enable/disable combat mode for.");
                return;
            }

            if (ServerGameManager.HasBuff(familiar, invulnerableBuff.ToIdentifier())) // remove and enable combat
            {
                BuffUtility.BuffSpawner buffSpawner = BuffUtility.BuffSpawner.Create(ServerGameManager);
                EntityCommandBuffer entityCommandBuffer = EntityCommandBufferSystem.CreateCommandBuffer();
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

                LocalizationService.HandleServerReply(EntityManager, userEntity.Read<User>(), "Familiar combat <color=green>enabled</color>.");
            }
            else // if not, disable combat
            {
                FactionReference factionReference = familiar.Read<FactionReference>();
                factionReference.FactionGuid._Value = ignoredFaction;
                familiar.Write(factionReference);

                AggroConsumer aggroConsumer = familiar.Read<AggroConsumer>();
                aggroConsumer.Active._Value = false;
                aggroConsumer.AggroTarget._Entity = Entity.Null;
                aggroConsumer.AlertTarget._Entity = Entity.Null;
                familiar.Write(aggroConsumer);

                Aggroable aggroable = familiar.Read<Aggroable>();
                aggroable.Value._Value = false;
                aggroable.DistanceFactor._Value = 0f;
                aggroable.AggroFactor._Value = 0f;
                familiar.Write(aggroable);

                ApplyBuffDebugEvent applyBuffDebugEvent = new()
                {
                    BuffPrefabGUID = invulnerableBuff,
                };

                FromCharacter fromCharacter = new()
                {
                    Character = familiar,
                    User = userEntity,
                };

                DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
                if (ServerGameManager.TryGetBuff(familiar, invulnerableBuff.ToIdentifier(), out Entity invlunerableBuff))
                {
                    if (invlunerableBuff.Has<LifeTime>())
                    {
                        var lifetime = invlunerableBuff.Read<LifeTime>();
                        lifetime.Duration = -1;
                        lifetime.EndAction = LifeTimeEndAction.None;
                        invlunerableBuff.Write(lifetime);
                    }
                }

                LocalizationService.HandleServerReply(EntityManager, userEntity.Read<User>(), "Familiar combat <color=red>disabled</color>.");
            }
        }
        else
        {
            LocalizationService.HandleServerReply(EntityManager, userEntity.Read<User>(), "No active familiar found to enable or disable combat mode for.");
        }
    }
}