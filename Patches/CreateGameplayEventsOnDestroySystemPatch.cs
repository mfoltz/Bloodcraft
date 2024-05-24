using Bloodcraft.Systems.Legacy;
using Bloodcraft.Systems.Professions;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using static Bloodcraft.Systems.Legacy.BloodSystem;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class CreateGameplayEventOnDestroySystemPatch
{
    const int BaseFishingXP = 100;

    [HarmonyPatch(typeof(CreateGameplayEventOnDestroySystem), nameof(CreateGameplayEventOnDestroySystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(CreateGameplayEventOnDestroySystem __instance)
    {
        NativeArray<Entity> entities = __instance.__query_1297357609_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (Plugin.BloodSystem.Value && entity.Has<SpellTarget>() && entity.Read<PrefabGUID>().GuidHash.Equals(-1106009274))
                {
                    Entity died = entity.Read<SpellTarget>().Target._Entity;
                    Entity killer = entity.Read<EntityOwner>().Owner;
                    BloodSystem.UpdateLegacy(killer, died);
                }
                if (Plugin.BloodSystem.Value && entity.Has<ChangeBloodOnGameplayEvent>() && entity.Has<EntityOwner>() && entity.Read<EntityOwner>().Owner.Has<PlayerCharacter>())
                {
                    ulong steamId = entity.Read<EntityOwner>().Owner.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
                    try
                    {
                        Entity player = entity.Read<EntityOwner>().Owner;
                        Blood blood = player.Read<Blood>();
                        BloodType bloodType = GetBloodTypeFromPrefab(blood.BloodType);
                        IBloodHandler bloodHandler = BloodHandlerFactory.GetBloodHandler(bloodType);
                        if (bloodHandler == null)
                        {
                            continue;
                        }
                        var legacyData = bloodHandler.GetLegacyData(steamId);
                        blood.Quality += legacyData.Key;
                        player.Write(blood);
                    }
                    catch (System.Exception ex)
                    {
                        Core.Log.LogError(ex);
                    }
                }

                PrefabGUID prefabGUID = entity.Read<PrefabGUID>();

                if (Plugin.ProfessionSystem.Value && prefabGUID.GuidHash.Equals(-1130746976)) // fishing travel to target, this indicates a succesful fishing event
                {
                    Entity character = entity.Read<EntityOwner>().Owner;
                    User user = character.Read<PlayerCharacter>().UserEntity.Read<User>();
                    ulong steamId = user.PlatformId;
                    PrefabGUID toProcess = new(0);
                    Entity target = entity.Read<Buff>().Target;

                    if (target.Has<DropTableBuffer>())
                    {
                        var dropTableBuffer = target.ReadBuffer<DropTableBuffer>();
                        if (!dropTableBuffer.IsEmpty)
                        {
                            toProcess = dropTableBuffer[0].DropTableGuid;
                        }
                    }
                    if (toProcess.GuidHash == 0)
                    {
                        continue;
                    }
                    IProfessionHandler handler = ProfessionHandlerFactory.GetProfessionHandler(toProcess, "");
                    int multiplier = ProfessionUtilities.GetFishingModifier(toProcess);
                    if (handler != null)
                    {
                        ProfessionSystem.SetProfession(user, steamId, BaseFishingXP * multiplier, handler);
                        ProfessionSystem.GiveProfessionBonus(toProcess, character, user, steamId, handler);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Core.Log.LogError($"Exited GameplayEventsSystem hook early: {e}");
        }
    }
}