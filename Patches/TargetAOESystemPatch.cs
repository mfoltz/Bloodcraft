namespace Bloodcraft.Patches;

/*
[HarmonyPatch]
internal static class TargetAOESystemPatch
{
    static readonly PrefabGUID militiaBombThrow = new(-2134151205);
    static readonly PrefabGUID captureBuff = new(548966542);

    [HarmonyPatch(typeof(TargetAOESystem), nameof(TargetAOESystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(TargetAOESystem __instance)
    {
        if (!Core.hasInitialized) return;
        if (!ConfigService.FamiliarSystem) return;

        NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (entity.Has<PrefabGUID>() && entity.GetOwner().IsPlayer())
                {
                    PrefabGUID prefabGUID = entity.Read<PrefabGUID>();

                    if (prefabGUID.Equals(militiaBombThrow))
                    {
                        //Core.Log.LogInfo("Militia bomb throw...");
                        entity.With((ref TargetAoE targetAoE) =>
                        {
                            //targetAoE.ThrowMaxHeightDiff = 1f;
                            //targetAoE.ThrowArcHeight = 0.5f;
                        });
                        if (entity.Has<SpawnPrefabOnGameplayEvent>())
                        {
                            var buffer = entity.ReadBuffer<SpawnPrefabOnGameplayEvent>();
                            var item = buffer[0];

                            item.SpawnPrefab = captureBuff;
                            item.SpellTarget = SetSpellTarget.BuffTarget;

                            buffer[0] = item;
                        }
                        if (entity.Has<CreateGameplayEventsOnDestroy>())
                        {
                            var buffer = entity.ReadBuffer<CreateGameplayEventsOnDestroy>();
                            var item = buffer[0];

                            item.DestroyReason = DestroyReason.OnHit;
                            item.Target = GameplayEventTarget.BuffTarget;

                            buffer[0] = item;
                        }
                        //if (entity.Has<GameplayEventListeners>()) entity.Remove<GameplayEventListeners>();
                    }
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }

    [HarmonyPatch(typeof(CharacterMenuOpenedSystem_Server), nameof(CharacterMenuOpenedSystem_Server.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(CharacterMenuOpenedSystem_Server __instance)
    {
        if (!Core.hasInitialized) return;

        NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (entity.Exists())
                {
                    //entity.LogComponentTypes();
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
}

[Info   :Bloodcraft] ===
[Info   :Bloodcraft] ProjectM.Network.FromCharacter
[Info   :Bloodcraft] ProjectM.Network.NetworkEventType
[Info   :Bloodcraft] ProjectM.Network.ReceiveNetworkEventTag
[Info   :Bloodcraft] ProjectM.Network.CharacterMenuOpenedEvent
[Info   :Bloodcraft] Unity.Entities.Simulate
[Info   :Bloodcraft] ===
*/