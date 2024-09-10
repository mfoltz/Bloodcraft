using Bloodcraft.Services;
using Bloodcraft.Systems.Professions;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class ReactToInventoryChangedSystemPatch
{
    [HarmonyPatch(typeof(ReactToInventoryChangedSystem), nameof(ReactToInventoryChangedSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ReactToInventoryChangedSystem __instance)
    {
        NativeArray<Entity> entities = __instance.__query_2096870024_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (var entity in entities)
            {
                if (!Core.hasInitialized) return; // probably going to change what should be a continue to a return at some point and maybe if I acknowldege it here will make that less likely to happen? >_>
                if (!ConfigService.ProfessionSystem) return;

                InventoryChangedEvent inventoryChangedEvent = entity.Read<InventoryChangedEvent>();
                Entity inventory = inventoryChangedEvent.InventoryEntity;

                if (!inventory.Exists()) continue;

                if (inventoryChangedEvent.ChangeType.Equals(InventoryChangedEventType.Obtained) && inventory.Has<InventoryConnection>())
                {
                    InventoryConnection inventoryConnection = inventory.Read<InventoryConnection>();

                    if (!inventoryConnection.InventoryOwner.Has<UserOwner>())
                    {
                        if (inventoryConnection.InventoryOwner.Has<EntityOwner>())
                        {
                            Entity inventoryOwner = inventoryConnection.InventoryOwner.GetOwner();
                            PrefabGUID ownerPrefab = inventoryOwner.Read<PrefabGUID>();
                            if (ownerPrefab.LookupName().ToLower().Contains("horse"))
                            {
                                entity.LogComponentTypes();
                                inventory.LogComponentTypes();
                            }
                        }
                        continue;
                    }

                    UserOwner userOwner = inventoryConnection.InventoryOwner.Read<UserOwner>();
                    Entity userEntity = userOwner.Owner._Entity;

                    if (!userEntity.Exists())
                    {
                        continue;
                    }

                    PrefabGUID itemPrefab = inventoryChangedEvent.Item;

                    if (inventoryChangedEvent.ItemEntity.Has<UpgradeableLegendaryItem>())
                    {
                        int tier = inventoryChangedEvent.ItemEntity.Read<UpgradeableLegendaryItem>().CurrentTier;
                        itemPrefab = inventoryChangedEvent.ItemEntity.ReadBuffer<UpgradeableLegendaryItemTiers>()[tier].TierPrefab;
                    }

                    User user = userEntity.Read<User>();
                    ulong steamId = user.PlatformId;

                    IProfessionHandler handler = ProfessionHandlerFactory.GetProfessionHandler(itemPrefab, "");
                    if (steamId.TryGetPlayerCraftingJobs(out var playerJobs) && playerJobs.TryGetValue(itemPrefab, out int credits) && credits > 0)
                    {
                        credits--;
                        if (credits == 0)
                        {
                            playerJobs.Remove(itemPrefab);
                        }
                        else
                        {
                            playerJobs[itemPrefab] = credits;
                        }

                        float ProfessionValue = 50f;
                        ProfessionValue *= ProfessionMappings.GetTierMultiplier(itemPrefab);

                        if (handler != null)
                        {
                            if (handler.GetProfessionName().ToLower().Contains("alchemy"))
                            {
                                ProfessionSystem.SetProfession(user, steamId, ProfessionValue * 3, handler);
                                continue;
                            }

                            ProfessionSystem.SetProfession(user, steamId, ProfessionValue, handler);
                            Entity itemEntity = inventoryChangedEvent.ItemEntity;

                            switch (handler)
                            {
                                case BlacksmithingHandler:
                                    if (itemEntity.Has<Durability>())
                                    {
                                        Durability durability = itemEntity.Read<Durability>();
                                        int level = handler.GetProfessionData(steamId).Key;
                                        durability.MaxDurability *= (1 + (float)level / (float)ConfigService.MaxProfessionLevel);
                                        durability.Value = durability.MaxDurability;
                                        itemEntity.Write(durability);
                                    }
                                    break;
                                case AlchemyHandler:
                                    break;
                                case EnchantingHandler:
                                    if (itemEntity.Has<Durability>())
                                    {
                                        Durability durability = itemEntity.Read<Durability>();
                                        int level = handler.GetProfessionData(steamId).Key;
                                        durability.MaxDurability *= (1 + (float)level / (float)ConfigService.MaxProfessionLevel);
                                        durability.Value = durability.MaxDurability;
                                        itemEntity.Write(durability);
                                    }
                                    break;
                                case TailoringHandler:
                                    if (itemEntity.Has<Durability>())
                                    {
                                        Durability durability = itemEntity.Read<Durability>();
                                        int level = handler.GetProfessionData(steamId).Key;
                                        durability.MaxDurability *= (1 + (float)level / (float)ConfigService.MaxProfessionLevel);
                                        durability.Value = durability.MaxDurability;
                                        itemEntity.Write(durability);
                                    }
                                    break;
                            }
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
