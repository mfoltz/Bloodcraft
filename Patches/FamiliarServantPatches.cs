using Bloodcraft.Services;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class FamiliarServantPatches
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static NetworkIdSystem.Singleton NetworkIdSystem => SystemService.NetworkIdSystem;

    static readonly bool _familiars = ConfigService.FamiliarSystem;

    [HarmonyPatch(typeof(EquipServantItemFromInventorySystem), nameof(EquipServantItemFromInventorySystem.OnUpdate))]
    [HarmonyPrefix]
    public static void OnUpdatePrefix(EquipServantItemFromInventorySystem __instance)
    {
        if (!Core.IsReady) return;
        else if (!_familiars) return;

        NativeArray<Entity> entities = __instance.EntityQueries[0].ToEntityArray(Allocator.Temp);
        NativeArray<EquipServantItemFromInventoryEvent> equipServantItemFromInventoryEvents = __instance.EntityQueries[0].ToComponentDataArray<EquipServantItemFromInventoryEvent>(Allocator.Temp);

        ComponentLookup<BlockFeedBuff> blockFeedBuffLookup = __instance.GetComponentLookup<BlockFeedBuff>();

        try
        {
            var networkIdLookupMap = NetworkIdSystem._NetworkIdLookupMap;

            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                EquipServantItemFromInventoryEvent equipServantItemFromInventoryEvent = equipServantItemFromInventoryEvents[i];

                NetworkId toEntity = equipServantItemFromInventoryEvent.ToEntity;
                NetworkId fromInventory = equipServantItemFromInventoryEvent.FromInventory;
                int slotIndex = equipServantItemFromInventoryEvent.SlotIndex;

                if (networkIdLookupMap.TryGetValue(toEntity, out Entity servant)
                    && blockFeedBuffLookup.HasComponent(servant)
                    && networkIdLookupMap.TryGetValue(fromInventory, out Entity inventory))
                {
                    if (InvalidFamiliarEquipment(inventory, slotIndex))
                    {
                        Core.Log.LogWarning($"[EquipServantItemFromInventorySystem] isLegendary!");
                        entity.Destroy(true);
                    }
                    else
                    {
                        Entity familiar = Familiars.GetServantFamiliar(servant);

                        if (familiar.Exists())
                        {
                            // Core.Log.LogWarning($"[EquipServantItemFromInventorySystem] Familiar servant equipped from inventory, refreshing stats...");
                            Buffs.RefreshStats(familiar);
                        }
                    }
                }
            }
        }
        finally
        {
            entities.Dispose();
            equipServantItemFromInventoryEvents.Dispose();
        }
    }
    static bool InvalidFamiliarEquipment(Entity inventory, int slotIndex)
    {
        if (InventoryUtilities.TryGetItemAtSlot(EntityManager, inventory, slotIndex, out InventoryBuffer item))
        {
            // bool result = item.ItemType.GetPrefabName().Contains(LEGENDARY);
            // return item.ItemType.GetPrefabName().Contains(LEGENDARY) || item.ItemEntity.GetEntityOnServer().IsAncestralWeapon();
            return item.ItemEntity.GetEntityOnServer().IsAncestralWeapon();
        }

        return false;
    }

    [HarmonyPatch(typeof(EquipServantItemSystem), nameof(EquipServantItemSystem.OnUpdate))]
    [HarmonyPrefix]
    public static void OnUpdatePrefix(EquipServantItemSystem __instance)
    {
        if (!Core.IsReady) return;
        else if (!_familiars) return;

        NativeArray<Entity> entities = __instance.EntityQueries[0].ToEntityArray(Allocator.Temp);
        NativeArray<EquipServantItemEvent> equipServantItemEvents = __instance.EntityQueries[0].ToComponentDataArray<EquipServantItemEvent>(Allocator.Temp);
        NativeArray<FromCharacter> fromCharacters = __instance.EntityQueries[0].ToComponentDataArray<FromCharacter>(Allocator.Temp);

        ComponentLookup<BlockFeedBuff> blockFeedBuffLookup = __instance.GetComponentLookup<BlockFeedBuff>();

        try
        {
            var networkIdLookupMap = NetworkIdSystem._NetworkIdLookupMap;

            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                EquipServantItemEvent equipServantItemEvent = equipServantItemEvents[i];
                FromCharacter fromCharacter = fromCharacters[i];

                NetworkId networkId = equipServantItemEvent.ToEntity;
                int slotIndex = equipServantItemEvent.SlotIndex;

                if (NetworkIdSystem._NetworkIdLookupMap.TryGetValue(networkId, out Entity servant)
                    && blockFeedBuffLookup.HasComponent(servant)
                    && InventoryUtilities.TryGetInventoryEntity(EntityManager, fromCharacter.Character, out Entity inventory))
                {
                    if (InvalidFamiliarEquipment(inventory, slotIndex))
                    {
                        Core.Log.LogWarning($"[EquipServantItemSystem] isLegendary!");
                        entity.Destroy(true);
                    }
                    else
                    {
                        Entity familiar = Familiars.GetServantFamiliar(servant);

                        if (familiar.Exists())
                        {
                            // Core.Log.LogWarning($"[EquipServantItemFromInventorySystem] Familiar servant equipped from inventory, refreshing stats...");
                            Buffs.RefreshStats(familiar);
                        }
                    }
                }
            }
        }
        finally
        {
            entities.Dispose();
            equipServantItemEvents.Dispose();
            fromCharacters.Dispose();
        }
    }

    [HarmonyPatch(typeof(UnEquipServantItemSystem), nameof(UnEquipServantItemSystem.OnUpdate))]
    [HarmonyPrefix]
    public static void OnUpdatePrefix(UnEquipServantItemSystem __instance)
    {
        if (!Core.IsReady) return;
        else if (!_familiars) return;

        NativeArray<UnequipServantItemEvent> unequipServantItemEvents = __instance.EntityQueries[0].ToComponentDataArray<UnequipServantItemEvent>(Allocator.Temp);

        ComponentLookup<BlockFeedBuff> blockFeedBuffLookup = __instance.GetComponentLookup<BlockFeedBuff>();

        try
        {
            for (int i = 0; i < unequipServantItemEvents.Length; i++)
            {
                UnequipServantItemEvent unequipServantItemEvent = unequipServantItemEvents[i];
                NetworkId networkId = unequipServantItemEvent.FromEntity;

                if (NetworkIdSystem._NetworkIdLookupMap.TryGetValue(networkId, out Entity servant)
                    && blockFeedBuffLookup.HasComponent(servant))
                {
                    Entity familiar = Familiars.GetServantFamiliar(servant);

                    if (familiar.Exists())
                    {
                        // Core.Log.LogWarning($"[UnEquipServantItemSystem] Familiar servant unequipped, refreshing stats...");
                        Buffs.RefreshStats(familiar);
                    }
                }
            }
        }
        finally
        {
            unequipServantItemEvents.Dispose();
        }
    }

    [HarmonyPatch(typeof(EquipmentTransferSystem), nameof(EquipmentTransferSystem.OnUpdate))]
    [HarmonyPrefix]
    public static void OnUpdatePrefix(EquipmentTransferSystem __instance)
    {
        if (!Core.IsReady) return;
        else if (!_familiars) return;

        NativeArray<Entity> entities = __instance.EntityQueries[0].ToEntityArray(Allocator.Temp);
        NativeArray<EquipmentToEquipmentTransferEvent> equipmentToEquipmentTransferEvents = __instance.EntityQueries[0].ToComponentDataArray<EquipmentToEquipmentTransferEvent>(Allocator.Temp);
        NativeArray<FromCharacter> fromCharacters = __instance.EntityQueries[0].ToComponentDataArray<FromCharacter>(Allocator.Temp);

        ComponentLookup<BlockFeedBuff> blockFeedBuffLookup = __instance.GetComponentLookup<BlockFeedBuff>();

        try
        {
            var networkIdLookupMap = NetworkIdSystem._NetworkIdLookupMap;

            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                EquipmentToEquipmentTransferEvent equipmentToEquipmentTransferEvent = equipmentToEquipmentTransferEvents[i];
                FromCharacter fromCharacter = fromCharacters[i];

                NetworkId networkId = equipmentToEquipmentTransferEvent.ToEntity;
                EquipmentType equipmentType = equipmentToEquipmentTransferEvent.EquipmentType;
                bool servantToCharacter = equipmentToEquipmentTransferEvent.ServantToCharacter;

                if (!servantToCharacter && NetworkIdSystem._NetworkIdLookupMap.TryGetValue(networkId, out Entity servant)
                    && blockFeedBuffLookup.HasComponent(servant)
                    && fromCharacter.Character.TryGetComponent(out Equipment equipment))
                {
                    if (equipment.GetEquipmentEntity(equipmentType).GetEntityOnServer().IsAncestralWeapon())
                    {
                        // Core.Log.LogWarning($"[EquipmentTransferSystem] isLegendary!");
                        entity.Destroy(true);
                    }
                    else
                    {
                        Entity familiar = Familiars.GetServantFamiliar(servant);

                        if (familiar.Exists())
                        {
                            // Core.Log.LogWarning($"[EquipmentTransferSystem] Familiar servant equipped, refreshing stats...");
                            Buffs.RefreshStats(familiar);
                        }
                    }
                }
                else if (servantToCharacter)
                {
                    Entity playerCharacter = fromCharacter.Character;
                    ulong steamId = playerCharacter.GetSteamId();

                    if (steamId.HasActiveFamiliar())
                    {
                        Entity familiar = Familiars.GetActiveFamiliar(playerCharacter);

                        // Core.Log.LogWarning($"[EquipmentTransferSystem] Familiar servant unequipped (?), refreshing stats...");
                        Buffs.RefreshStats(familiar);
                    }
                }
            }
        }
        finally
        {
            entities.Dispose();
            equipmentToEquipmentTransferEvents.Dispose();
            fromCharacters.Dispose();
        }
    }

    [HarmonyPatch(typeof(ServantPowerSystem), nameof(ServantPowerSystem.OnUpdate))]
    [HarmonyPrefix]
    public static void OnUpdatePrefix(ServantPowerSystem __instance)
    {
        if (!Core.IsReady) return;
        else if (!_familiars) return;

        NativeArray<Entity> entities = __instance.EntityQueries[0].ToEntityArray(Allocator.Temp);

        ComponentLookup<BlockFeedBuff> blockFeedBuffLookup = __instance.GetComponentLookup<BlockFeedBuff>();

        try
        {
            foreach (Entity entity in entities)
            {
                if (blockFeedBuffLookup.HasComponent(entity))
                {
                    // Core.Log.LogWarning($"[ServantPowerSystem] BlockFeedBuff servant...");
                    Entity familiar = Familiars.GetServantFamiliar(entity);

                    if (familiar.Exists())
                    {
                        // Core.Log.LogWarning($"[ServantPowerSystem] Servant familiar found!");
                        Familiars.SyncFamiliarServant(familiar, entity);
                    }
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }

    [HarmonyPatch(typeof(MoveItemBetweenInventoriesSystem), nameof(MoveItemBetweenInventoriesSystem.OnUpdate))]
    [HarmonyPrefix]
    public static void OnUpdatePrefix(MoveItemBetweenInventoriesSystem __instance)
    {
        if (!Core.IsReady) return;
        else if (!_familiars) return;

        using NativeAccessor<Entity> entities = __instance._MoveItemBetweenInventoriesEventQuery.ToEntityArrayAccessor();
        using NativeAccessor<MoveItemBetweenInventoriesEvent> moveItemBetweenInventoriesEvents = __instance._MoveItemBetweenInventoriesEventQuery.ToComponentDataArrayAccessor<MoveItemBetweenInventoriesEvent>();
        using NativeAccessor<FromCharacter> fromCharacters = __instance._MoveItemBetweenInventoriesEventQuery.ToComponentDataArrayAccessor<FromCharacter>();

        ComponentLookup<BlockFeedBuff> blockFeedBuffLookup = __instance.GetComponentLookup<BlockFeedBuff>();

        try
        {
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                MoveItemBetweenInventoriesEvent moveItemBetweenInventoriesEvent = moveItemBetweenInventoriesEvents[i];
                FromCharacter fromCharacter = fromCharacters[i];

                NetworkId toInventory = moveItemBetweenInventoriesEvent.ToInventory;
                int slotIndex = moveItemBetweenInventoriesEvent.FromSlot;

                // Core.Log.LogWarning($"[MoveItemBetweenInventoriesSystem]"); // yep, this system

                if (NetworkIdSystem._NetworkIdLookupMap.TryGetValue(toInventory, out Entity toInventoryEntity))
                {
                    Entity playerCharacter = fromCharacter.Character;
                    // Entity servant = Familiars.GetFamiliarServant(playerCharacter);
                    // Entity servantInventory = InventoryUtilities.TryGetInventoryEntity(EntityManager, servant, out Entity inventory) ? inventory : Entity.Null;

                    // if (servant.Exists()) Core.DumpEntity(Core.Server, servant); // playerCharacter, yeah that makes sense

                    /*
                    if (toInventoryEntity.Exists())
                    {
                        Core.Log.LogWarning($"[MoveItemBetweenInventoriesSystem] {toInventoryEntity} | {servantInventory}");
                        Core.DumpEntity(Core.Server, toInventoryEntity); // appears to be the servant entity
                    }
                    */

                    if (!blockFeedBuffLookup.HasComponent(toInventoryEntity))
                    {
                        // Core.Log.LogWarning($"[MoveItemBetweenInventoriesSystem] No BlockFeedBuff component on servant or inventory entities don't match!");
                        continue;
                    }
                    else if (InvalidFamiliarEquipment(playerCharacter, slotIndex))
                    {
                        // Core.Log.LogWarning($"[MoveItemBetweenInventoriesSystem] Invalid equipment!");
                        entity.Destroy(true);
                    }
                    else
                    {
                        Entity familiar = Familiars.GetActiveFamiliar(playerCharacter);

                        if (familiar.Exists())
                        {
                            // Core.Log.LogWarning($"[MoveItemBetweenInventoriesSystem] Familiar servant equipped, refreshing stats...");
                            Buffs.RefreshStats(familiar);
                        }
                    }
                }
                else
                {
                    // Core.Log.LogWarning($"[MoveItemBetweenInventoriesSystem] No NetworkId found for toInventory!");
                }
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogError($"[MoveItemBetweenInventoriesSystem] Error in OnUpdatePrefix: {ex}");
        }
    }
}
