using Bloodcraft.Systems.Familiars;
using Bloodcraft.Systems.Legacy;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using static Bloodcraft.Systems.Experience.LevelingSystem;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class StatChangeSystemPatches
{
    static readonly Random Random = new();
    static DebugEventsSystem DebugEventsSystem => Core.DebugEventsSystem;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static GameModeType GameMode => Core.ServerGameSettings.GameModeType;
    static readonly bool Parties = Plugin.Parties.Value;
    static readonly bool Familiars = Plugin.FamiliarSystem.Value;
    static readonly bool PreventFriendlyFire = Plugin.PreventFriendlyFire.Value;
    static readonly bool Classes = Plugin.SoftSynergies.Value || Plugin.HardSynergies.Value;
    static readonly bool OnHitEffects = Plugin.ClassSpellSchoolOnHitEffects.Value;
    static readonly float OnHitChance = Plugin.OnHitProcChance.Value;
    static readonly PrefabGUID pvpProtBuff = new(1111481396);

    [HarmonyPatch(typeof(StatChangeMutationSystem), nameof(StatChangeMutationSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(StatChangeMutationSystem __instance)
    {
        NativeArray<Entity> entities = __instance._StatChangeEventQuery.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!Core.hasInitialized) continue;

                if (Plugin.BloodSystem.Value && Plugin.BloodQualityBonus.Value && entity.Has<StatChangeEvent>() && entity.Has<BloodQualityChange>())
                {
                    StatChangeEvent statChangeEvent = entity.Read<StatChangeEvent>();
                    BloodQualityChange bloodQualityChange = entity.Read<BloodQualityChange>();
                    Blood blood = statChangeEvent.Entity.Read<Blood>();
                    BloodSystem.BloodType bloodType = BloodSystem.GetBloodTypeFromPrefab(bloodQualityChange.BloodType);
                    ulong steamID = statChangeEvent.Entity.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;

                    IBloodHandler bloodHandler = BloodHandlerFactory.GetBloodHandler(bloodType);
                    var bloodQualityBuff = statChangeEvent.Entity.ReadBuffer<BloodQualityBuff>();

                    if (bloodHandler == null)
                    {
                        continue;
                    }

                    float legacyKey = bloodHandler.GetLegacyData(steamID).Value;

                    if (Plugin.PrestigeSystem.Value && Core.DataStructures.PlayerPrestiges.TryGetValue(steamID, out var prestiges) && prestiges.TryGetValue(BloodSystem.BloodPrestigeMap[bloodType], out var bloodPrestige) && bloodPrestige > 0)
                    {
                        legacyKey = (float)bloodPrestige * Plugin.PrestigeBloodQuality.Value;
                        if (legacyKey > 0)
                        {
                            bloodQualityChange.Quality += legacyKey;
                            bloodQualityChange.ForceReapplyBuff = true;
                            entity.Write(bloodQualityChange);
                        }
                    }
                    else if (!Plugin.PrestigeSystem.Value)
                    {
                        if (legacyKey > 0)
                        {
                            bloodQualityChange.Quality += legacyKey;
                            bloodQualityChange.ForceReapplyBuff = true;
                            entity.Write(bloodQualityChange);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogInfo(ex);
        }
        finally
        {
            entities.Dispose();
        }
    }
    
    [HarmonyPatch(typeof(DealDamageSystem), nameof(DealDamageSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(DealDamageSystem __instance)
    {
        NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!Core.hasInitialized) continue;

                DealDamageEvent dealDamageEvent = entity.Read<DealDamageEvent>();

                if (dealDamageEvent.MainType != MainDamageType.Physical && dealDamageEvent.MainType != MainDamageType.Spell) continue;

                if (dealDamageEvent.Target.TryGetComponent(out PlayerCharacter target))
                {
                    if (Parties && PreventFriendlyFire && !GameMode.Equals(GameModeType.PvE) && dealDamageEvent.SpellSource.TryGetComponent(out EntityOwner entityOwner) && entityOwner.Owner.TryGetComponent(out PlayerCharacter source))
                    {
                        Dictionary<ulong, HashSet<string>> playerAlliances = Core.DataStructures.PlayerParties;
                        string targetName = target.Name.Value;
                        string sourceName = source.Name.Value;
                        ulong steamId = source.UserEntity.Read<User>().PlatformId;

                        if (playerAlliances.Values.Any(set => set.Contains(targetName) && set.Contains(sourceName)))
                        {
                            Core.EntityManager.DestroyEntity(entity);
                        }
                    }
                    else if (Familiars && dealDamageEvent.SpellSource.TryGetComponent(out EntityOwner familiarEntityOwner) && familiarEntityOwner.Owner.TryGetComponent(out Follower follower) && follower.Followed._Value.Has<PlayerCharacter>())
                    {
                        if (GameMode.Equals(GameModeType.PvE)) // always stop in PvE
                        {
                            Entity familiar = FamiliarSummonSystem.FamiliarUtilities.FindPlayerFamiliar(follower.Followed._Value);
                            if (familiar != Entity.Null)
                            {
                                //Core.Log.LogInfo($"Destroying familiar damage event PvE...");
                                Core.EntityManager.DestroyEntity(entity);
                            }
                        }
                        else if (ServerGameManager.TryGetBuff(dealDamageEvent.Target, pvpProtBuff.ToIdentifier(), out Entity _)) // account for KindredArenas <3
                        {
                            Entity familiar = FamiliarSummonSystem.FamiliarUtilities.FindPlayerFamiliar(follower.Followed._Value);
                            if (familiar != Entity.Null)
                            {
                                //Core.Log.LogInfo($"Destroying familiar damage event PvP protected...");
                                Core.EntityManager.DestroyEntity(entity);
                            }
                        }
                        else if (Parties && PreventFriendlyFire) // check for parties in PvP 
                        {
                            Dictionary<ulong, HashSet<string>> playerAlliances = Core.DataStructures.PlayerParties;
                            string targetName = target.Name.Value;
                            string ownerName = follower.Followed._Value.Read<PlayerCharacter>().Name.Value;

                            Entity familiar = FamiliarSummonSystem.FamiliarUtilities.FindPlayerFamiliar(follower.Followed._Value);
                            if (familiar != Entity.Null && playerAlliances.Values.Any(set => set.Contains(targetName) && set.Contains(ownerName)))
                            {
                                //Core.Log.LogInfo($"Destroying familiar damage event Parties & PreventFriendlyFire...");
                                Core.EntityManager.DestroyEntity(entity);
                            }
                        }
                    }
                }
                else if (OnHitEffects && Classes && dealDamageEvent.SpellSource.TryGetComponent(out EntityOwner entityOwner) && entityOwner.Owner.TryGetComponent(out PlayerCharacter source))
                {
                    ulong steamId = source.UserEntity.Read<User>().PlatformId;
                    if (Core.DataStructures.PlayerClasses.TryGetValue(steamId, out var classData) && classData.Keys.Count == 0) continue;
                    PlayerClasses playerClass = GetPlayerClass(steamId);
                    //Core.Log.LogInfo($"Player class: {playerClass}");
                    if (Random.NextDouble() <= OnHitChance)
                    {
                        PrefabGUID prefabGUID = ClassOnHitDebuffMap[playerClass];
                        FromCharacter fromCharacter = new()
                        {
                            Character = dealDamageEvent.Target,
                            User = source.UserEntity
                        };

                        ApplyBuffDebugEvent applyBuffDebugEvent = new()
                        {
                            BuffPrefabGUID = prefabGUID,
                        };
                        if (ServerGameManager.HasBuff(dealDamageEvent.Target, prefabGUID.ToIdentifier()))
                        {
                            applyBuffDebugEvent.BuffPrefabGUID = ClassOnHitEffectMap[playerClass];
                            fromCharacter.Character = entityOwner.Owner;
                            //Core.Log.LogInfo($"Tier 2 on hit effect proc'd...");
                            DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
                        }
                        else
                        {
                            //Core.Log.LogInfo($"Tier 1 on hit effect proc'd...");
                            DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
                        }
                    }
                }
            }
            //Core.DamageEventSystem.DealDamageSystemEvents = __instance._Query.ToComponentDataArray<DealDamageEvent>(Allocator.TempJob);
            //Core.DamageEventSystem.OnUpdate();
        }
        catch (Exception ex)
        {
            Core.Log.LogInfo(ex);
        }
        finally
        {
            //if (Core.DamageEventSystem.DealDamageSystemEvents.IsCreated) Core.DamageEventSystem.DealDamageSystemEvents.Dispose();
            entities.Dispose();
        }
    }
}
