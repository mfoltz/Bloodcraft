using Bloodcraft.Services;
using Bloodcraft.Systems.Professions;
using Bloodcraft.Systems.Quests;
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
    const float ProfessionBaseXP = 50f;

    [HarmonyPatch(typeof(ReactToInventoryChangedSystem), nameof(ReactToInventoryChangedSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ReactToInventoryChangedSystem __instance)
    {
        if (!Core.hasInitialized) return;
        if (!ConfigService.ProfessionSystem) return;

        NativeArray<Entity> entities = __instance.__query_2096870024_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (var entity in entities)
            {
                if (!entity.TryGetComponent(out InventoryChangedEvent inventoryChangedEvent)) continue;

                Entity inventory = inventoryChangedEvent.InventoryEntity;
                if (inventory.TryGetComponent(out InventoryConnection inventoryConnection) && inventoryChangedEvent.ChangeType.Equals(InventoryChangedEventType.Obtained))
                {
                    User user;
                    ulong steamId;

                    if (inventoryConnection.InventoryOwner.IsPlayer())
                    {
                        user = inventoryConnection.InventoryOwner.Read<PlayerCharacter>().UserEntity.Read<User>();
                        steamId = user.PlatformId;

                        if (!DealDamageSystemPatch.LastDamageTime.ContainsKey(steamId)) continue;
                        else if (DealDamageSystemPatch.LastDamageTime.TryGetValue(steamId, out DateTime lastDamageTime) && (DateTime.UtcNow - lastDamageTime).TotalSeconds < 0.10f)
                        {
                            if (steamId.TryGetPlayerQuests(out var quests)) QuestSystem.ProcessQuestProgress(quests, inventoryChangedEvent.Item, inventoryChangedEvent.Amount, user);
                            continue;
                        }

                        continue;
                    }

                    if (!inventoryConnection.InventoryOwner.TryGetComponent(out UserOwner userOwner)) continue;
                    Entity userEntity = userOwner.Owner._Entity;

                    PrefabGUID itemPrefab = inventoryChangedEvent.Item;
                    Entity itemEntity = inventoryChangedEvent.ItemEntity;

                    if (itemEntity.Has<UpgradeableLegendaryItem>())
                    {
                        int tier = itemEntity.Read<UpgradeableLegendaryItem>().CurrentTier;
                        itemPrefab = itemEntity.ReadBuffer<UpgradeableLegendaryItemTiers>()[tier].TierPrefab;
                    }

                    if (!userEntity.TryGetComponent(out user)) continue;
                    steamId = user.PlatformId;

                    if (steamId.TryGetPlayerCraftingJobs(out Dictionary<PrefabGUID, int> playerJobs) && playerJobs.TryGetValue(itemPrefab, out int credits) && credits > 0)
                    {
                        credits--;

                        if (credits == 0) playerJobs.Remove(itemPrefab);
                        else playerJobs[itemPrefab] = credits;

                        float professionXP = ProfessionBaseXP * ProfessionMappings.GetTierMultiplier(itemPrefab);
                        IProfessionHandler handler = ProfessionHandlerFactory.GetProfessionHandler(itemPrefab, "");

                        if (handler != null)
                        {
                            if (handler.GetProfessionName().Contains("Alchemy")) professionXP *= 3;
                            if (steamId.TryGetPlayerQuests(out var quests)) QuestSystem.ProcessQuestProgress(quests, itemPrefab, 1, user);

                            ProfessionSystem.SetProfession(inventoryConnection.InventoryOwner, user.LocalCharacter.GetEntityOnServer(), steamId, professionXP, handler);
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
