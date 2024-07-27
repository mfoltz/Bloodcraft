using Bloodcraft.Systems.Familiars;
using Bloodcraft.Systems.Legacy;
using HarmonyLib;
using ProjectM;
using ProjectM.Behaviours;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using static Bloodcraft.Systems.Experience.PlayerLevelingUtilities;
using Random = System.Random;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class StatChangeSystemPatches
{
    static readonly Random Random = new();

    static EntityManager EntityManager => Core.EntityManager;
    static DebugEventsSystem DebugEventsSystem => Core.DebugEventsSystem;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    static readonly PrefabGUID sctCombat = new(781573820);
    static GameModeType GameMode => Core.ServerGameSettings.GameModeType;
    static readonly bool Parties = Plugin.Parties.Value;
    static readonly bool Familiars = Plugin.FamiliarSystem.Value;
    static readonly bool PreventFriendlyFire = Plugin.PreventFriendlyFire.Value;
    static readonly bool Classes = Plugin.SoftSynergies.Value || Plugin.HardSynergies.Value;
    static readonly bool OnHitEffects = Plugin.ClassSpellSchoolOnHitEffects.Value;
    static readonly float OnHitChance = Plugin.OnHitProcChance.Value;
    static readonly PrefabGUID pvpProtBuff = new(1111481396);
    static readonly PrefabGUID stormShield03 = new(1095865904);
    static readonly PrefabGUID stormShield02 = new(-1192885497);
    static readonly PrefabGUID stormShield01 = new(1044565673);

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
                    LegacyUtilities.BloodType bloodType = LegacyUtilities.GetBloodTypeFromPrefab(bloodQualityChange.BloodType);
                    ulong steamID = statChangeEvent.Entity.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;

                    IBloodHandler bloodHandler = BloodHandlerFactory.GetBloodHandler(bloodType);
                    var bloodQualityBuff = statChangeEvent.Entity.ReadBuffer<BloodQualityBuff>();

                    if (bloodHandler == null)
                    {
                        continue;
                    }

                    float legacyKey = bloodHandler.GetLegacyData(steamID).Value;

                    if (Plugin.PrestigeSystem.Value && Core.DataStructures.PlayerPrestiges.TryGetValue(steamID, out var prestiges) && prestiges.TryGetValue(LegacyUtilities.BloodPrestigeMap[bloodType], out var bloodPrestige) && bloodPrestige > 0)
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

                if (dealDamageEvent.MainType != MainDamageType.Physical && dealDamageEvent.MainType != MainDamageType.Spell && dealDamageEvent.MainType != MainDamageType.Holy) continue;

                if (dealDamageEvent.Target.TryGetComponent(out PlayerCharacter target))
                {
                    if (!dealDamageEvent.MainType.Equals(MainDamageType.Holy) && Parties && PreventFriendlyFire && !GameMode.Equals(GameModeType.PvE) && dealDamageEvent.SpellSource.TryGetComponent(out EntityOwner entityOwner) && entityOwner.Owner.TryGetComponent(out PlayerCharacter source))
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
                            Entity familiar = FamiliarSummonUtilities.FamiliarUtilities.FindPlayerFamiliar(follower.Followed._Value);
                            if (familiar != Entity.Null)
                            {
                                //Core.Log.LogInfo($"Destroying familiar damage event PvE...");
                                Core.EntityManager.DestroyEntity(entity);
                            }
                        }
                        else if (ServerGameManager.TryGetBuff(dealDamageEvent.Target, pvpProtBuff.ToIdentifier(), out Entity _)) // account for KindredArenas <3
                        {
                            Entity familiar = FamiliarSummonUtilities.FamiliarUtilities.FindPlayerFamiliar(follower.Followed._Value);
                            if (familiar != Entity.Null)
                            {
                                //Core.Log.LogInfo($"Destroying familiar damage event PvP protected...");
                                Core.EntityManager.DestroyEntity(entity);
                            }
                        }
                        else if (Parties && PreventFriendlyFire) // check for parties in PvP 
                        {
                            Dictionary<ulong, HashSet<string>> playerParties = Core.DataStructures.PlayerParties;
                            string targetName = target.Name.Value;
                            string ownerName = follower.Followed._Value.Read<PlayerCharacter>().Name.Value;

                            Entity familiar = FamiliarSummonUtilities.FamiliarUtilities.FindPlayerFamiliar(follower.Followed._Value);
                            if (familiar != Entity.Null && playerParties.Values.Any(set => set.Contains(targetName) && set.Contains(ownerName)))
                            {
                                //Core.Log.LogInfo($"Destroying familiar damage event Parties & PreventFriendlyFire...");
                                Core.EntityManager.DestroyEntity(entity);
                            }
                        }
                    }
                }
                else if (!dealDamageEvent.MainType.Equals(MainDamageType.Holy) && OnHitEffects && Classes && dealDamageEvent.SpellSource.TryGetComponent(out EntityOwner entityOwner) && entityOwner.Owner.TryGetComponent(out PlayerCharacter source))
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
                            if (playerClass.Equals(PlayerClasses.DemonHunter))
                            {
                                if (ServerGameManager.TryGetBuff(entityOwner.Owner, stormShield01.ToIdentifier(), out Entity firstBuff))
                                {
                                    firstBuff.Write(new LifeTime { Duration = 5f, EndAction = LifeTimeEndAction.Destroy });
                                }
                                else if (ServerGameManager.TryGetBuff(entityOwner.Owner, stormShield02.ToIdentifier(), out Entity secondBuff))
                                {
                                    secondBuff.Write(new LifeTime { Duration = 5f, EndAction = LifeTimeEndAction.Destroy });
                                }
                                else if (ServerGameManager.TryGetBuff(entityOwner.Owner, stormShield03.ToIdentifier(), out Entity thirdBuff))
                                {
                                    thirdBuff.Write(new LifeTime { Duration = 5f, EndAction = LifeTimeEndAction.Destroy });
                                }
                                else
                                {
                                    DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
                                }
                            }
                            else
                            {
                                DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
                            }
                        }
                        else
                        {
                            //Core.Log.LogInfo($"Tier 1 on hit effect proc'd...");
                            DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
                            if (ServerGameManager.TryGetBuff(dealDamageEvent.Target, applyBuffDebugEvent.BuffPrefabGUID.ToIdentifier(), out Entity buff))
                            {
                                buff.Write(new EntityOwner { Owner = entityOwner.Owner });
                            }
                        }
                    }
                    /*
                    Entity familiar = FamiliarSummonUtilities.FamiliarUtilities.FindPlayerFamiliar(entityOwner.Owner);
                    if (EntityManager.Exists(familiar))
                    {
                        // want to have fam attack thing here if doesn't already have a formerly set target?
                        //Core.Log.LogInfo($"Main Factor: {dealDamageEvent.MainFactor} | Main Type: {dealDamageEvent.MainType.ToString()} | Modifier: {dealDamageEvent.Modifier} | Raw Damage: {dealDamageEvent.RawDamage} | Raw Percent: {dealDamageEvent.RawDamagePercent} | Resource Modifier: {dealDamageEvent.ResourceModifier}");
                        DealDamageParameters dealDamageParameters = new()
                        {
                            MainFactor = 1f,
                            MainType = MainDamageType.Physical,
                            RawDamageValue = 0f,
                            RawDamagePercent = 0f,
                            DealDamageFlags = (int)DealDamageFlag.None,
                            MaterialModifiers = EntityTypeModifiers.Default,
                            ResourceModifier = 1f
                        };
                        ServerGameManager.DealDamage(dealDamageEvent.Target, familiar, dealDamageParameters, 100f);
                        //Core.Log.LogInfo("Familiar provoked~");
                    }
                    */
                }
                /*
                else if (Familiars && dealDamageEvent.SpellSource.TryGetComponent(out EntityOwner familiarEntityOwner) && familiarEntityOwner.Owner.TryGetComponent(out Follower follower) && follower.Followed._Value.Has<PlayerCharacter>())
                {
                    //ScrollingCombatTextMessage.Create
                    ServerGameManager.CreateScrollingCombatText(dealDamageEvent.RawDamage, sctCombat, dealDamageEvent.Target.Read<Translation>().Value, follower.Followed._Value, default);
                }
                */
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
