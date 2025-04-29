using BepInEx.Unity.IL2CPP.Hook;
using Bloodcraft.Services;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Scripting;
using System.Reflection;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

/*
[HarmonyPatch]
internal static class PlayerCombatBuffSystemPatch
{
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;

    static readonly GameModeType _gameMode = SystemService.ServerGameSettingsSystem._Settings.GameModeType;

    static readonly bool _familiars = ConfigService.FamiliarSystem;

    [HarmonyPatch(typeof(PlayerCombatBuffSystem_InitialApplication_Aggro), nameof(PlayerCombatBuffSystem_InitialApplication_Aggro.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ref SystemState state)
    {
        if (!Core._initialized) return;
        else if (!_familiars) return;

        Core.Log.LogWarning($"[PlayerCombatBuffSystem_InitialApplication_Aggro Patch]");
        NativeArray<Entity> entities = state.EntityQueries.get_Item(0).ToEntityArray(Allocator.Temp);

        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out InverseAggroEvents.Added inverseAggroEvent)) continue;
                else if (inverseAggroEvent.Producer.TryGetPlayer(out Entity playerCharacter))
                {
                    Entity familiar = Familiars.GetActiveFamiliar(playerCharacter);

                    if (familiar.Exists() && Familiars.EligibleForCombat(familiar))
                    {
                        Familiars.AddToFamiliarAggroBuffer(playerCharacter, familiar, [inverseAggroEvent.Consumer]);
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

        // Core.Log.LogWarning($"[PlayerCombatBuffSystem_Reapplication Patch]");
        NativeArray<Entity> entities = __instance.EntityQueries[0].ToEntityArray(Allocator.Temp);

        try
        {
            foreach (Entity entity in entities)
            {
                Entity familiar = Familiars.GetActiveFamiliar(entity);

                if (familiar.Exists() && Familiars.EligibleForCombat(familiar))
                {
                    Familiars.SyncAggro(entity, familiar);

                    foreach (InverseAggroBufferElement element in buffer)
                    {
                        Familiars.AddToFamiliarAggroBuffer(entity, familiar, TODO);
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

#nullable enable
internal static class PlayerCombatBuffSystemAggroDetour
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    unsafe delegate void PlayerCombatBuffSystemAggroHandler(IntPtr _this, ref SystemState state);
    static PlayerCombatBuffSystemAggroHandler? _playerCombatBuffSystemAggro;
    static INativeDetour? _playerCombatBuffSystemAggroDetour;
    public static unsafe void Initialize()
    {
        try
        {
            // _playerCombatBuffSystemAggroDetour = NativeDetour.Create(typeof(PlayerCombatBuffSystem_InitialApplication_Aggro), "OnUpdate", HandlePlayerCombatBuffAggro, out _playerCombatBuffSystemAggro);
            _playerCombatBuffSystemAggroDetour = NativeDetour.Create(
            typeof(PlayerCombatBuffSystem_InitialApplication_Aggro)
                .GetNestedType("ObjectNInternalAbstractSealedInPoDeInUnique",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static),
            "Invoke",
            HandlePlayerCombatBuffAggro,
            out _playerCombatBuffSystemAggro
            );
        }
        catch (Exception e)
        {
            Core.Log.LogError($"Failed to route {nameof(PlayerCombatBuffSystemAggroDetour)}: {e}");
        }
    }
    static void HandlePlayerCombatBuffAggro(IntPtr _this, ref SystemState state)
    {
        Core.Log.LogWarning($"[PlayerCombatBuffSystemAggro Detour]");
        NativeArray<Entity> entities = state.EntityQueries.get_Item(0).ToEntityArray(Allocator.Temp);

        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out InverseAggroEvents.Added inverseAggroEvent)) continue;
                else if (inverseAggroEvent.Producer.TryGetPlayer(out Entity playerCharacter))
                {
                    Entity familiar = Familiars.GetActiveFamiliar(playerCharacter);

                    if (familiar.Exists() && Familiars.EligibleForCombat(familiar))
                    {
                        Familiars.AddToFamiliarAggroBuffer(playerCharacter, familiar, [inverseAggroEvent.Consumer]);
                    }
                }
            }
        }
        finally
        {
            entities.Dispose();
        }

        _playerCombatBuffSystemAggro!(_this, ref state);
    }
}
*/
