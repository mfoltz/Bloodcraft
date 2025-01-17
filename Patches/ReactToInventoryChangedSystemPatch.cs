using Bloodcraft.Services;
using Bloodcraft.Systems.Professions;
using Bloodcraft.Systems.Quests;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class ReactToInventoryChangedSystemPatch
{
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    static readonly bool _professions = ConfigService.ProfessionSystem;
    static readonly bool _quests = ConfigService.QuestSystem;

    static readonly int _maxProfessionLevel = ConfigService.MaxProfessionLevel;

    const float BASE_PROFESSION_XP = 50f;
    const float FAKE_DELAY = 0.75f;

    [HarmonyPatch(typeof(ReactToInventoryChangedSystem), nameof(ReactToInventoryChangedSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ReactToInventoryChangedSystem __instance)
    {
        if (!Core._initialized) return;
        else if (!_professions && !_quests) return;

        NativeArray<InventoryChangedEvent> inventoryChangedEvents = __instance.__query_2096870024_0.ToComponentDataArray<InventoryChangedEvent>(Allocator.Temp);
        try
        {
            foreach (InventoryChangedEvent inventoryChangedEvent in inventoryChangedEvents)
            {
                Entity inventory = inventoryChangedEvent.InventoryEntity;

                if (inventoryChangedEvent.ChangeType.Equals(InventoryChangedEventType.Obtained) && inventory.TryGetComponent(out InventoryConnection inventoryConnection))
                {
                    if (_quests && inventoryConnection.InventoryOwner.IsPlayer())
                    {
                        User questUser = inventoryConnection.InventoryOwner.Read<PlayerCharacter>().UserEntity.Read<User>();

                        if (!DealDamageSystemPatch.LastDamageTime.ContainsKey(questUser.PlatformId)) continue;
                        else if (DealDamageSystemPatch.LastDamageTime.TryGetValue(questUser.PlatformId, out DateTime lastDamageTime) && (DateTime.UtcNow - lastDamageTime).TotalSeconds < 0.10f)
                        {
                            if (questUser.PlatformId.TryGetPlayerQuests(out var quests)) QuestSystem.ProcessQuestProgress(quests, inventoryChangedEvent.Item, inventoryChangedEvent.Amount, questUser);
                        }
                        else continue;
                    }
                    else if (inventoryConnection.InventoryOwner.TryGetComponent(out UserOwner userOwner) && userOwner.Owner.GetEntityOnServer().TryGetComponent(out User user))
                    {
                        Entity craftingStation = inventoryConnection.InventoryOwner;
                        ulong steamId = user.PlatformId;

                        Dictionary<ulong, User> clanMembers = [];
                        Entity clanEntity = user.ClanEntity.GetEntityOnServer();

                        PrefabGUID itemPrefabGUID = inventoryChangedEvent.Item;
                        Entity itemPrefab = inventoryChangedEvent.ItemEntity;
                        string itemName = itemPrefabGUID.GetPrefabName();

                        if (itemPrefab.Has<UpgradeableLegendaryItem>())
                        {
                            int tier = itemPrefab.Read<UpgradeableLegendaryItem>().CurrentTier;
                            itemPrefabGUID = itemPrefab.ReadBuffer<UpgradeableLegendaryItemTiers>()[tier].TierPrefab;
                        }

                        if (!clanEntity.Exists()) clanMembers.TryAdd(user.PlatformId, user);
                        else if (ServerGameManager.TryGetBuffer<SyncToUserBuffer>(clanEntity, out var clanUserBuffer) && !clanUserBuffer.IsEmpty)
                        {
                            foreach (SyncToUserBuffer syncToUser in clanUserBuffer)
                            {
                                if (syncToUser.UserEntity.TryGetComponent(out User clanUser))
                                {
                                    clanMembers.TryAdd(clanUser.PlatformId, clanUser);
                                }
                            }
                        }

                        foreach (var keyValuePair in clanMembers)
                        {
                            steamId = keyValuePair.Key;
                            user = keyValuePair.Value;

                            if (CraftingSystemPatches.ValidatedCraftingJobs.TryGetValue(steamId, out var craftingStationJobs) && craftingStationJobs.TryGetValue(craftingStation, out var craftingJobs) && craftingJobs.TryGetValue(itemPrefabGUID, out int jobs) && jobs > 0)
                            {
                                --jobs;

                                if (jobs == 0) craftingJobs.Remove(itemPrefabGUID);
                                else craftingJobs[itemPrefabGUID] = jobs;

                                if (_quests && steamId.TryGetPlayerQuests(out var quests)) QuestSystem.ProcessQuestProgress(quests, itemPrefabGUID, 1, user);

                                if (_professions)
                                {
                                    float professionXP = BASE_PROFESSION_XP * ProfessionMappings.GetTierMultiplier(itemPrefabGUID);
                                    float delay = FAKE_DELAY;

                                    IProfessionHandler handler = ProfessionHandlerFactory.GetProfessionHandler(itemPrefabGUID, "");
                                    if (handler != null)
                                    {
                                        if (handler.GetProfessionName().Contains("Alchemy")) professionXP *= 3;
                                        if (itemName.EndsWith("Bloodwine")) professionXP *= 2;

                                        ProfessionSystem.SetProfession(inventoryConnection.InventoryOwner, user.LocalCharacter.GetEntityOnServer(), steamId, professionXP, handler, ref delay);
                                        switch (handler)
                                        {
                                            case BlacksmithingHandler:
                                                if (itemPrefab.Has<Durability>())
                                                {
                                                    Durability durability = itemPrefab.Read<Durability>();
                                                    int level = handler.GetProfessionData(steamId).Key;
                                                    durability.MaxDurability *= (1 + level / (float)_maxProfessionLevel);
                                                    durability.Value = durability.MaxDurability;
                                                    itemPrefab.Write(durability);
                                                }
                                                //EquipmentManager.ApplyEquipmentStats(steamId, itemEntity);
                                                break;
                                            case AlchemyHandler:
                                                break;
                                            case EnchantingHandler:
                                                if (itemPrefab.Has<Durability>())
                                                {
                                                    Durability durability = itemPrefab.Read<Durability>();
                                                    int level = handler.GetProfessionData(steamId).Key;
                                                    durability.MaxDurability *= (1 + level / (float)_maxProfessionLevel);
                                                    durability.Value = durability.MaxDurability;
                                                    itemPrefab.Write(durability);
                                                }
                                                //EquipmentManager.ApplyEquipmentStats(steamId, itemEntity);
                                                break;
                                            case TailoringHandler:
                                                if (itemPrefab.Has<Durability>())
                                                {
                                                    Durability durability = itemPrefab.Read<Durability>();
                                                    int level = handler.GetProfessionData(steamId).Key;
                                                    durability.MaxDurability *= (1 + level / (float)_maxProfessionLevel);
                                                    durability.Value = durability.MaxDurability;
                                                    itemPrefab.Write(durability);
                                                }
                                                //EquipmentManager.ApplyEquipmentStats(steamId, itemEntity);
                                                break;
                                        }
                                    }
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
            inventoryChangedEvents.Dispose();
        }
    }
}
