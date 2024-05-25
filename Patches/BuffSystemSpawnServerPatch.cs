using Bloodcraft.Systems.Legacy;
using Bloodcraft.Systems.Professions;
using HarmonyLib;
using Il2CppSystem.Buffers;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using static Bloodcraft.Systems.Legacy.BloodSystem;

namespace Bloodcraft.Patches;

[HarmonyPatch]
public class BuffPatch
{
    [HarmonyPatch(typeof(BuffSystem_Spawn_Server), nameof(BuffSystem_Spawn_Server.OnUpdate))]
    [HarmonyPrefix]
    private static void OnUpdatePrefix(BuffSystem_Spawn_Server __instance)
    {
        NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                PrefabGUID prefabGUID = entity.Read<PrefabGUID>();
                if (Plugin.ProfessionSystem.Value && prefabGUID.LookupName().ToLower().Contains("consumable") && entity.Read<Buff>().Target.Has<PlayerCharacter>())
                {
                    IProfessionHandler handler = ProfessionHandlerFactory.GetProfessionHandler(prefabGUID, "alchemy");
                    ulong steamId = entity.Read<Buff>().Target.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
                    int level = handler.GetExperienceData(steamId).Key;
                    if (entity.Has<LifeTime>())
                    {
                        LifeTime lifeTime = entity.Read<LifeTime>();
                        lifeTime.Duration *= (1 + level / Plugin.MaxProfessionLevel.Value);
                        entity.Write(lifeTime);
                    }
                    if (entity.Has<ModifyUnitStatBuff_DOTS>())
                    {
                        var buffer = entity.ReadBuffer<ModifyUnitStatBuff_DOTS>();
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            ModifyUnitStatBuff_DOTS statBuff = buffer[i];
                            statBuff.Value *= (1 + level / Plugin.MaxProfessionLevel.Value);
                            buffer[i] = statBuff;
                        }
                    }
                }

                if (Plugin.BloodSystem.Value && prefabGUID.GuidHash.Equals(366323518) && entity.Read<Buff>().Target.Has<PlayerCharacter>()) // feed execute kills
                {
                    ulong steamId = entity.Read<Buff>().Target.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
                    Entity died = entity.Read<SpellTarget>().Target._Entity;
                    Entity killer = entity.Read<EntityOwner>().Owner;
                    BloodSystem.UpdateLegacy(killer, died);
                }
                
            }
        }
        catch (System.Exception ex)
        {
            Core.Log.LogError(ex);
        }
        finally
        {
            entities.Dispose();
        }
    }
    
    [HarmonyPatch(typeof(CreateGameplayEvents_OnAbilityCast), nameof(CreateGameplayEvents_OnAbilityCast.OnUpdate))]
    [HarmonyPostfix]
    private static void OnUpdatePostix(CreateGameplayEvents_OnAbilityCast __instance)
    {
        NativeArray<Entity> entities = __instance.__query_1365518405_2.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (Plugin.BloodSystem.Value && entity.Has<AbilityPostCastFinishedEvent>())
                {
                    AbilityPostCastFinishedEvent abilityPostCastFinishedEvent = entity.Read<AbilityPostCastFinishedEvent>();
                    PrefabGUID prefabGUID = abilityPostCastFinishedEvent.AbilityGroup.Read<PrefabGUID>();
                    Entity character = abilityPostCastFinishedEvent.Character;
                    if (character.Has<PlayerCharacter>() && prefabGUID.GuidHash.Equals(974235336)) // blood potion ability group
                    {
                        ulong steamId = character.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
                        Blood blood = character.Read<Blood>();
                        BloodType bloodType = GetBloodTypeFromPrefab(blood.BloodType);
                        IBloodHandler bloodHandler = BloodHandlerFactory.GetBloodHandler(bloodType);
                        if (bloodHandler == null)
                        {
                            continue;
                        }
                        var legacyData = bloodHandler.GetLegacyData(steamId);
                        blood.Quality += legacyData.Key;
                        character.Write(blood);
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Core.Log.LogError(ex);
        }
        finally
        {
            entities.Dispose();
        }
    }
}
