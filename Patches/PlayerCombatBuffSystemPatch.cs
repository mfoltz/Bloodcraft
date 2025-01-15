using Bloodcraft.Services;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Scripting;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class PlayerCombatBuffSystemPatch
{
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;

    static readonly GameModeType _gameMode = SystemService.ServerGameSettingsSystem._Settings.GameModeType;

    static readonly bool _familiars = ConfigService.FamiliarSystem;

    [HarmonyPatch(typeof(PlayerCombatBuffSystem_OnAggro), nameof(PlayerCombatBuffSystem_OnAggro.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(PlayerCombatBuffSystem_OnAggro __instance)
    {
        if (!Core._initialized) return;
        else if (!_familiars) return;

        NativeArray<Entity> entities = __instance.EntityQueries[0].ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out InverseAggroEvents.Added inverseAggroEvent)) continue;
                else if (inverseAggroEvent.Producer.TryGetPlayer(out Entity playerCharacter))
                {
                    Entity familiar = Familiars.FindPlayerFamiliar(playerCharacter);

                    if (familiar.Exists() && Familiars.IsEligibleForCombat(familiar))
                    {
                        Familiars.AddToFamiliarAggroBuffer(familiar, inverseAggroEvent.Consumer);
                    }
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }

    [HarmonyPatch(typeof(PlayerCombatBuffSystem_Reapplication), nameof(PlayerCombatBuffSystem_Reapplication.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(PlayerCombatBuffSystem_Reapplication __instance)
    {
        if (!Core._initialized) return;
        else if (!_familiars) return;

        NativeArray<Entity> entities = __instance.EntityQueries[0].ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (ServerGameManager.TryGetBuffer<InverseAggroBufferElement>(entity, out var buffer))
                {
                    Entity familiar = Familiars.FindPlayerFamiliar(entity);

                    if (familiar.Exists() && Familiars.IsEligibleForCombat(familiar))
                    {
                        foreach (InverseAggroBufferElement element in buffer)
                        {
                            Familiars.AddToFamiliarAggroBuffer(familiar, element.Entity);
                        }
                    }
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
}