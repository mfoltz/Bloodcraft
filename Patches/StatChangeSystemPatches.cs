using Bloodcraft.Systems.Legacy;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class StatChangeSystemPatches
{
    static readonly bool PlayerAlliances = Plugin.PlayerAlliances.Value;

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
    /*
    [HarmonyPatch(typeof(DealDamageSystem), nameof(DealDamageSystem.ShouldTakeDamage))]
    [HarmonyPrefix]
    static void ShouldTakeDamagePrefix(ref DealDamageSystem.SystemInput input, Entity target, Entity dealer, DealDamageTargetTypeEnum dealerType, DealDamageTargetTypeEnum targetType, out ProjectM.Debugging.DealDamageResultEnum dealDamageResultEnum)
    {
        //NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);
        Dictionary<ulong, HashSet<string>> playerAlliances = Core.DataStructures.PlayerAlliances;

        dealDamageResultEnum = DealDamageResultEnum.Success;

        ulong steamId = dealer.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
        string targetName = target.TryGetComponent(out EntityOwner targetEntityOwner) && targetEntityOwner.Owner.TryGetComponent(out PlayerCharacter targetCharacter) ? targetCharacter.Name.Value : "";

        if (playerAlliances.TryGetValue(steamId, out var alliance) && alliance.Contains(targetName))
        {
            dealDamageResultEnum = ProjectM.Debugging.DealDamageResultEnum.Invalid;

            Core.Log.LogInfo($"Player from A {steamId} is in an alliance with {targetName}");
        }
        else if (playerAlliances.Values.Any(set => set.Contains(targetName)))
        {
            dealDamageResultEnum = ProjectM.Debugging.DealDamageResultEnum.Invalid;

            Core.Log.LogInfo($"Player from B {steamId} is in an alliance with {targetName}");
        } 
    } 
    */
}
