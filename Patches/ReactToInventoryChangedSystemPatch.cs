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
    static SystemService SystemService => Core.SystemService;
    static JewelSpawnSystem JewelSpawnSystem => SystemService.JewelSpawnSystem;

    static readonly System.Random _random = new();

    static readonly bool _professions = ConfigService.ProfessionSystem;
    static readonly bool _quests = ConfigService.QuestSystem;
    static readonly bool _extraRecipes = ConfigService.ExtraRecipes;

    const int MAX_PROFESSION_LEVEL = 100;
    const float BASE_PROFESSION_XP = 50f;
    const float ALCHEMY_FACTOR = 3f;

    const float BLOOD_POTION_FACTOR = 25f;
    const float MERLOT_BONUS = 2f;
    const float MAX_BLOOD_QUALITY = 100f;

    const float SCT_DELAY = 0.75f;

    static readonly PrefabGUID _itemJewelTemplate = new(1075994038);
    static readonly PrefabGUID _advancedGrinder = new(-178579946);
    static readonly PrefabGUID _gemCuttingTable = new(-21483617);

    static readonly List<PrefabGUID> _jewelTemplates = 
    [
        new(1412786604),  // Item_Jewel_Unholy_T04
        new(2023809276),  // Item_Jewel_Storm_T04
        new(97169184),    // Item_Jewel_Illusion_T04
        new(-147757377),  // Item_Jewel_Frost_T04
        new(-1796954295), // Item_Jewel_Chaos_T04
        new(271061481)    // Item_Jewel_Blood_T04
    ];

    [HarmonyPatch(typeof(ReactToInventoryChangedSystem), nameof(ReactToInventoryChangedSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ReactToInventoryChangedSystem __instance)
    {
        if (!Core._initialized) return;
        else if (!_professions && !_quests && !_extraRecipes) return;

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
                    }
                    else if (inventoryConnection.InventoryOwner.TryGetComponent(out UserOwner userOwner) && userOwner.Owner.GetEntityOnServer().TryGetComponent(out User user))
                    {
                        Entity castleWorkstation = inventoryConnection.InventoryOwner;
                        ulong steamId = user.PlatformId;

                        Dictionary<ulong, User> clanMembers = [];
                        Entity clanEntity = user.ClanEntity.GetEntityOnServer();
                        
                        PrefabGUID itemPrefabGuid = inventoryChangedEvent.Item;
                        Entity itemEntity = inventoryChangedEvent.ItemEntity;
                        string itemName = itemPrefabGuid.GetPrefabName();

                        if (_extraRecipes && castleWorkstation.GetPrefabGuid().Equals(_gemCuttingTable) && itemPrefabGuid.Equals(_itemJewelTemplate))
                        {
                            SpawnPrimalJewel(inventory);

                            continue;
                        }
                        
                        if (itemEntity.Has<UpgradeableLegendaryItem>())
                        {
                            int tier = itemEntity.Read<UpgradeableLegendaryItem>().CurrentTier;
                            itemPrefabGuid = itemEntity.ReadBuffer<UpgradeableLegendaryItemTiers>()[tier].TierPrefab;
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

                            if (CraftingSystemPatches.ValidatedCraftingJobs.TryGetValue(steamId, out var craftingStationJobs) && craftingStationJobs.TryGetValue(castleWorkstation, out var craftingJobs) && craftingJobs.TryGetValue(itemPrefabGuid, out int jobs) && jobs > 0)
                            {
                                --jobs;

                                if (jobs == 0) craftingJobs.Remove(itemPrefabGuid);
                                else craftingJobs[itemPrefabGuid] = jobs;

                                if (_quests && steamId.TryGetPlayerQuests(out var quests)) QuestSystem.ProcessQuestProgress(quests, itemPrefabGuid, 1, user);

                                if (_professions)
                                {
                                    float professionXP = BASE_PROFESSION_XP * ProfessionMappings.GetTierMultiplier(itemPrefabGuid);
                                    float delay = SCT_DELAY;

                                    IProfessionHandler handler = ProfessionHandlerFactory.GetProfessionHandler(itemPrefabGuid, "");
                                    switch (handler)
                                    {
                                        case BlacksmithingHandler:
                                            ProfessionSystem.SetProfession(inventoryConnection.InventoryOwner, user.LocalCharacter.GetEntityOnServer(), steamId, professionXP, handler, ref delay);
                                            EquipmentManager.ApplyEquipmentStats(steamId, itemEntity);
                                            break;

                                        case AlchemyHandler:
                                            if (itemEntity.TryGetComponent(out StoredBlood storedBlood))
                                            {
                                                bool merlot = itemName.EndsWith("Bloodwine");
                                                float alchemyMultiplier;

                                                if (merlot)
                                                {
                                                    alchemyMultiplier = Math.Min(1f, storedBlood.BloodQuality / BLOOD_POTION_FACTOR) * MERLOT_BONUS;
                                                }
                                                else
                                                {
                                                    alchemyMultiplier = Math.Min(1f, storedBlood.BloodQuality / BLOOD_POTION_FACTOR);
                                                }

                                                professionXP *= alchemyMultiplier;
                                            }
                                            else professionXP *= ALCHEMY_FACTOR;

                                            ProfessionSystem.SetProfession(inventoryConnection.InventoryOwner, user.LocalCharacter.GetEntityOnServer(), steamId, professionXP, handler, ref delay);
                                            break;

                                        case EnchantingHandler:
                                            ProfessionSystem.SetProfession(inventoryConnection.InventoryOwner, user.LocalCharacter.GetEntityOnServer(), steamId, professionXP, handler, ref delay);
                                            EquipmentManager.ApplyEquipmentStats(steamId, itemEntity);
                                            break;

                                        case TailoringHandler:
                                            ProfessionSystem.SetProfession(inventoryConnection.InventoryOwner, user.LocalCharacter.GetEntityOnServer(), steamId, professionXP, handler, ref delay);
                                            EquipmentManager.ApplyEquipmentStats(steamId, itemEntity);
                                            break;

                                        default:
                                            break;
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
    static void SpawnPrimalJewel(Entity inventory)
    {
        if (ServerGameManager.TryRemoveInventoryItem(inventory, _itemJewelTemplate, 1))
        {
            PrefabGUID primalJewel = _jewelTemplates.ElementAt(_random.Next(_jewelTemplates.Count));

            AddItemResponse addResponse = ServerGameManager.TryAddInventoryItem(inventory, primalJewel, 1);
            if (addResponse.Success && addResponse.NewEntity.TryGetComponent(out JewelInstance jewelInstance))
            {
                Entity jewelEntity = addResponse.NewEntity;

                JewelSpawnSystem.UninitializedJewelAbility uninitializedJewel = new()
                {
                    AbilityGuid = jewelInstance.Ability.HasValue() ? jewelInstance.Ability : jewelInstance.OverrideAbilityType,
                    JewelEntity = jewelEntity,
                    JewelTier = jewelInstance.TierIndex
                };

                JewelSpawnSystem.InitializeSpawnedJewel(uninitializedJewel, false);
            }
        }
    }
}
