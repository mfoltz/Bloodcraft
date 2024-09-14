using Bloodcraft.Services;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;

namespace Bloodcraft.Patches;

/*
[HarmonyPatch]
internal static class UnusedTestPatches
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static DebugEventsSystem DebugEventsSystem => SystemService.DebugEventsSystem;

    static readonly bool Classes = ConfigService.HardSynergies || ConfigService.SoftSynergies;

    static readonly PrefabGUID captureBuff = new(548966542);
    static readonly Dictionary<Entity, ModifiableEntity> SpellMods = [];
    
    [HarmonyPatch(typeof(SpellModSyncSystem_Server), nameof(SpellModSyncSystem_Server.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(SpellModSyncSystem_Server __instance)
    {
        if (!Core.hasInitialized) return;
        if (!Classes) return;

        NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (entity.TryGetComponent(out FromCharacter fromCharacter) && fromCharacter.Character.TryGetComponent(out Equipment equipment))
                {
                    if (equipment.WeaponSlot.SlotEntity.GetEntityOnServer().Has<LegendaryItemInstance>())
                    {
                        Core.Log.LogInfo("Found legendary item on player in prefix, attempting to avoid bug by saving current spell mod and overwriting this one in postfix...");
                        Entity abilityGroup = ServerGameManager.GetAbilityGroup(fromCharacter.Character, 3);

                        if (!abilityGroup.Exists()) continue;
                        else if (ServerGameManager.TryGetBuffer<AbilityGroupSlotBuffer>(fromCharacter.Character, out var buffer))
                        {
                            PrefabGUID abilityPrefab = abilityGroup.Read<PrefabGUID>();
                            foreach (AbilityGroupSlotBuffer slot in buffer)
                            {
                                if (slot.BaseAbilityGroupOnSlot.Equals(abilityPrefab))
                                {
                                    Core.Log.LogInfo("AbilityGroup entity found matching shift prefab, removing and storing spell mod...");
                                    Entity slotEntity = slot.GroupSlotEntity.GetEntityOnServer();
                                    if (slotEntity.TryGetComponent(out AbilityGroupSlot abilityGroupSlot))
                                    {
                                        if (abilityGroupSlot.SpellModsSource._Value.Exists())
                                        {
                                            Core.Log.LogInfo("Found spell mod, storing...");
                                            SpellMods.TryAdd(slotEntity, abilityGroupSlot.SpellModsSource);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Core.Log.LogInfo($"SpellModSyncSystem_Server Prefix Exception: {e}");
        }
        finally
        {
            entities.Dispose();
        }
    }

    [HarmonyPatch(typeof(SpellModSyncSystem_Server), nameof(SpellModSyncSystem_Server.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdatePostfix(SpellModSyncSystem_Server __instance)
    {
        if (!Core.hasInitialized) return;
        if (!Classes) return;

        NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (entity.TryGetComponent(out FromCharacter fromCharacter) && fromCharacter.Character.TryGetComponent(out Equipment equipment))
                {
                    if (equipment.WeaponSlot.SlotEntity.GetEntityOnServer().Has<LegendaryItemInstance>())
                    {
                        Core.Log.LogInfo("Found legendary item on player in postfix, attempting to overwrite spell mod from weapon...");
                        Entity abilityGroup = ServerGameManager.GetAbilityGroup(fromCharacter.Character, 3);

                        if (!abilityGroup.Exists()) continue;
                        else if (ServerGameManager.TryGetBuffer<AbilityGroupSlotBuffer>(fromCharacter.Character, out var buffer))
                        {
                            PrefabGUID abilityPrefab = abilityGroup.Read<PrefabGUID>();
                            foreach (AbilityGroupSlotBuffer slot in buffer)
                            {
                                if (slot.BaseAbilityGroupOnSlot.Equals(abilityPrefab))
                                {
                                    Core.Log.LogInfo("AbilityGroup entity found matching shift prefab, adding spell mod back...");
                                    Entity slotEntity = slot.GroupSlotEntity.GetEntityOnServer();
                                    if (slotEntity.TryGetComponent(out AbilityGroupSlot abilityGroupSlot))
                                    {
                                        if (SpellMods.TryGetValue(slotEntity, out ModifiableEntity spellMod))
                                        {
                                            abilityGroupSlot.SpellModsSource = spellMod;
                                            slotEntity.Write(spellMod);
                                            SpellMods.Remove(slotEntity);
                                            Core.Log.LogInfo("Spell mod overwritten in postfix! Maybe...");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Core.Log.LogInfo($"SpellModSyncSystem_Server Postfix Exception: {e}");
        }
        finally
        {
            entities.Dispose();
        }
    }
    
    static readonly PrefabGUID charmProjectile = new(1652376207);

    [HarmonyPatch(typeof(HitCastColliderSystem_OnUpdate), nameof(HitCastColliderSystem_OnUpdate.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(HitCastColliderSystem_OnUpdate __instance)
    {
        if (!Core.hasInitialized) return;
        if (!ConfigService.FamiliarSystem) return;

        NativeArray<Entity> entities = __instance.__query_911162766_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities) // probably want to add capture mode or w/e to toggle for player to acivate this
            {
                // AoETarget could work if on the event here
                // HitCasts also
                // ColliderCastHit
                //__instance._HitCastColliderSystem._HitsCached
                // guess watch for the entity from my player then try to do the above?
                //HitTrigger
                if (!entity.GetOwner().TryGetPlayer(out Entity player)) continue;
                else if (entity.Read<PrefabGUID>().Equals(charmProjectile))
                {
                    if (entity.Has<ApplyBuffOnGameplayEvent>()) entity.Remove<ApplyBuffOnGameplayEvent>();
                    if (ServerGameManager.TryGetBuffer<HitTrigger>(entity, out var hitTrigger) && !hitTrigger.IsEmpty)
                    {
                        Core.Log.LogInfo("HitTrigger not empty...");
                        foreach (HitTrigger hit in hitTrigger)
                        {
                            if (hit.Target.Exists())
                            {
                                AttemptCapture(hit.Target, player);
                            }
                        }
                    }
                    else if (ServerGameManager.TryGetBuffer<CreateGameplayEventsOnHit>(entity, out var eventsOnHit) && (eventsOnHit[0].ColliderCastIndex != -1 || eventsOnHit[1].ColliderCastIndex != -1))
                    {
                        int castIndex = -1;
                        try
                        {
                            if (eventsOnHit[1].ColliderCastIndex != -1)
                            {
                                castIndex = eventsOnHit[1].ColliderCastIndex;
                            }
                            else if (eventsOnHit[0].ColliderCastIndex != -1)
                            {
                                castIndex = eventsOnHit[0].ColliderCastIndex;
                            }

                            if (castIndex != -1)
                            {
                                ColliderCastHit colliderCastHit = __instance._HitCastColliderSystem._HitsCached.ElementAt(castIndex);
                                Entity target = colliderCastHit._Entity_k__BackingField;
                                if (target.Exists())
                                {
                                    AttemptCapture(target, player);
                                }
                            }
                            else
                            {
                                Core.Log.LogInfo("No cast index found... (-1)");
                            }
                        }
                        catch (Exception ex)
                        {
                            Core.Log.LogInfo($"Error on attempt capture: {ex}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogInfo($"Error in HitCastColliderSystem_OnUpdate: {ex}");
        }
        finally
        {
            entities.Dispose();
        }
    }
    static void AttemptCapture(Entity target, Entity player)
    {
        ApplyBuffDebugEvent applyBuffDebugEvent = new()
        {
            BuffPrefabGUID = captureBuff,
            Who = target.Read<NetworkId>()
        };

        FromCharacter fromCharacter = new()
        {
            Character = player,
            User = player.Read<PlayerCharacter>().UserEntity
        };
        
        Core.Log.LogInfo("Attempting capture...");
        DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
    }
}
*/
