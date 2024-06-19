using Bloodcraft.Systems.Familiars;
using Bloodcraft.Systems.Legacy;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class StatChangeSystemPatches
{
    static GameModeType GameMode => Core.ServerGameSettings.GameModeType;
    static readonly bool PlayerAlliances = Plugin.PlayerAlliances.Value;
    static readonly bool Familiars = Plugin.FamiliarSystem.Value;
    static readonly bool PreventFriendlyFire = Plugin.PreventFriendlyFire.Value;

    [HarmonyPatch(typeof(StatChangeMutationSystem), nameof(StatChangeMutationSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(StatChangeMutationSystem __instance)
    {
        NativeArray<Entity> entities = __instance._StatChangeEventQuery.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
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
                DealDamageEvent dealDamageEvent = entity.Read<DealDamageEvent>();

                //dealDamageEvent.SpellSource.Read<EntityOwner>().Owner.LogComponentTypes();

                //Core.Log.LogInfo(GameMode.ToString());

                if (dealDamageEvent.Target.TryGetComponent(out PlayerCharacter target))
                {
                    if (PlayerAlliances && PreventFriendlyFire && !GameMode.Equals(GameModeType.PvE) && dealDamageEvent.SpellSource.TryGetComponent(out EntityOwner entityOwner) && entityOwner.Owner.TryGetComponent(out PlayerCharacter source))
                    {
                        Dictionary<ulong, HashSet<string>> playerAlliances = Core.DataStructures.PlayerAlliances;
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
                                Core.EntityManager.DestroyEntity(entity);
                            }
                        }
                        else if (PlayerAlliances && PreventFriendlyFire && GameMode.Equals(GameModeType.PvP)) // check for alliance in PvP
                        {
                            Dictionary<ulong, HashSet<string>> playerAlliances = Core.DataStructures.PlayerAlliances;
                            string targetName = target.Name.Value;
                            string ownerName = follower.Followed._Value.Read<PlayerCharacter>().Name.Value;

                            Entity familiar = FamiliarSummonSystem.FamiliarUtilities.FindPlayerFamiliar(follower.Followed._Value);
                            if (familiar != Entity.Null && playerAlliances.Values.Any(set => set.Contains(targetName) && set.Contains(ownerName)))
                            {
                                Core.EntityManager.DestroyEntity(entity);
                            }
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
    /*
    [HarmonyPatch(typeof(AbilityCastStarted_SetupAbilityTargetSystem_Shared), nameof(AbilityCastStarted_SetupAbilityTargetSystem_Shared.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(AbilityCastStarted_SetupAbilityTargetSystem_Shared __instance)
    {
        NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!PlayerAlliances) continue;
                AbilityCastStartedEvent abilityCastStartedEvent = entity.Read<AbilityCastStartedEvent>();
                //entity.LogComponentTypes();
                if (abilityCastStartedEvent.a.TryGetComponent(out EntityOwner targetEntityOwner) && targetEntityOwner.Owner.TryGetComponent(out PlayerCharacter target))
                {
                    Core.Log.LogInfo("Checking target...");
                    if (dealDamageEvent.SpellSource.TryGetComponent(out EntityOwner entityOwner) && entityOwner.Owner.TryGetComponent(out PlayerCharacter source))
                    {
                        Core.Log.LogInfo("Checking source...");
                        Dictionary<ulong, HashSet<string>> playerAlliances = Core.DataStructures.PlayerAlliances;
                        string targetName = target.Name.Value;
                        string sourceName = source.Name.Value;
                        ulong steamId = source.UserEntity.Read<User>().PlatformId;
                        Core.Log.LogInfo($"Checking alliance for {sourceName} and {targetName}...");
                        if (Core.DataStructures.PlayerAlliances.TryGetValue(steamId, out var alliance) && alliance.Contains(targetName)) //check if owner is alliance leader
                        {
                            //entity.LogComponentTypes();
                            Core.Log.LogInfo("Destroying damage entity (target in leader alliance)");
                            Core.EntityManager.DestroyEntity(entity);
                            //DestroyUtility.Destroy(Core.EntityManager, entity, DestroyDebugReason.None);

                        }
                        else // check if otherwise allied
                        {
                            if (playerAlliances.Values.Any(set => set.Contains(targetName) && set.Contains(sourceName)))
                            {
                                entity.LogComponentTypes();
                                Core.Log.LogInfo("Destroying damage entity (target and source in same alliance)");
                                Core.EntityManager.DestroyEntity(entity);
                                //DestroyUtility.Destroy(Core.EntityManager, entity, DestroyDebugReason.None);
                            }
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
    */
}
