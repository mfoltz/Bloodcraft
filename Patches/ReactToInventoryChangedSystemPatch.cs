using Bloodcraft.Interfaces;
using Bloodcraft.Resources;
using Bloodcraft.Services;
using Bloodcraft.Systems.Professions;
using Bloodcraft.Systems.Quests;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using System.Collections;
using System.Collections.Concurrent;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

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

    static readonly WaitForSeconds _delay = new(PRIMAL_DELAY);

    const int MAX_PROFESSION_LEVEL = 100;
    const float BASE_PROFESSION_XP = 50f;
    const float ALCHEMY_FACTOR = 3f;

    const float BLOOD_POTION_FACTOR = 25f;
    const float MERLOT_BONUS = 2f;
    const float MAX_BLOOD_QUALITY = 100f;

    const float ONYX_TEAR_FACTOR = 8f;

    const float SCT_DELAY = 0.75f;
    const float PRIMAL_DELAY = 0.5f;

    static readonly PrefabGUID _itemJewelTemplate = new(1075994038);
    static readonly PrefabGUID _advancedGrinder = new(-178579946);
    static readonly PrefabGUID _gemCuttingTable = new(-21483617);
    static readonly PrefabGUID _onyxTear = PrefabGUIDs.Item_Ingredient_OnyxTear;

    static readonly List<PrefabGUID> _jewelTemplates =
    [
        new(1412786604),  // Item_Jewel_Unholy_T04
        new(2023809276),  // Item_Jewel_Storm_T04
        new(97169184),    // Item_Jewel_Illusion_T04
        new(-147757377),  // Item_Jewel_Frost_T04
        new(-1796954295), // Item_Jewel_Chaos_T04
        new(271061481)    // Item_Jewel_Blood_T04
    ];

    static readonly List<PrefabGUID> _perfectGems =
    [
        PrefabGUIDs.Item_Ingredient_Gem_Amethyst_T04,
        PrefabGUIDs.Item_Ingredient_Gem_Ruby_T04,
        PrefabGUIDs.Item_Ingredient_Gem_Sapphire_T04,
        PrefabGUIDs.Item_Ingredient_Gem_Emerald_T04,
        PrefabGUIDs.Item_Ingredient_Gem_Topaz_T04,
        PrefabGUIDs.Item_Ingredient_Gem_Miststone_T04
    ];

    static readonly Dictionary<PrefabGUID, PrefabGUID> _perfectGemPrimals = new()
    {
        { PrefabGUIDs.Item_Ingredient_Gem_Emerald_T04, PrefabGUIDs.Item_Jewel_Unholy_T04 },
        { PrefabGUIDs.Item_Ingredient_Gem_Topaz_T04, PrefabGUIDs.Item_Jewel_Storm_T04 },
        { PrefabGUIDs.Item_Ingredient_Gem_Miststone_T04, PrefabGUIDs.Item_Jewel_Illusion_T04 },
        { PrefabGUIDs.Item_Ingredient_Gem_Sapphire_T04, PrefabGUIDs.Item_Jewel_Frost_T04 },
        { PrefabGUIDs.Item_Ingredient_Gem_Amethyst_T04, PrefabGUIDs.Item_Jewel_Chaos_T04 },
        { PrefabGUIDs.Item_Ingredient_Gem_Ruby_T04, PrefabGUIDs.Item_Jewel_Blood_T04 }
    };

    [HarmonyPatch(typeof(ReactToInventoryChangedSystem), nameof(ReactToInventoryChangedSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ReactToInventoryChangedSystem __instance)
    {
        if (!Core.IsReady) return;
        else if (!_professions && !_quests && !_extraRecipes) return;

        NativeArray<InventoryChangedEvent> inventoryChangedEvents = __instance.EntityQueries[0].ToComponentDataArray<InventoryChangedEvent>(Allocator.Temp);
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

                        if (StatChangeSystemPatch.LastDamageTime.TryGetValue(questUser.PlatformId, out DateTime lastDamageTime) && (DateTime.UtcNow - lastDamageTime).TotalSeconds < 0.1f)
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
                            SpawnPrimalJewel(castleWorkstation, inventory);
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

                                    if (itemName.Contains("Elixir")) professionXP *= 2f;

                                    // Core.Log.LogWarning($"Profession Crafting - {itemPrefabGuid.GetPrefabName()}");
                                    IProfession handler = ProfessionFactory.GetProfession(itemPrefabGuid);

                                    if (handler == null || handler.GetProfessionEnum().IsDisabled()) continue;

                                    switch (handler)
                                    {
                                        case BlacksmithingProfession:
                                            ProfessionSystem.SetProfession(inventoryConnection.InventoryOwner, user.LocalCharacter.GetEntityOnServer(), steamId, professionXP, handler, ref delay);
                                            EquipmentQualityManager.ApplyPlayerEquipmentStats(steamId, itemEntity);
                                            break;
                                        case AlchemyProfession:
                                            if (itemEntity.TryGetComponent(out StoredBlood storedBlood))
                                            {
                                                bool merlot = itemName.Contains("Bloodwine");
                                                float alchemyMultiplier;

                                                if (merlot)
                                                {
                                                    alchemyMultiplier = 5f;
                                                }
                                                else
                                                {
                                                    alchemyMultiplier = 2.5f;
                                                }

                                                float bloodQualityBonus = 1f + (storedBlood.BloodQuality / 100f);
                                                alchemyMultiplier += bloodQualityBonus;
                                                professionXP *= alchemyMultiplier;
                                            }
                                            if (itemPrefabGuid.Equals(_onyxTear))
                                            {
                                                professionXP *= ONYX_TEAR_FACTOR;
                                            }
                                            else professionXP *= ALCHEMY_FACTOR;
                                            ProfessionSystem.SetProfession(inventoryConnection.InventoryOwner, user.LocalCharacter.GetEntityOnServer(), steamId, professionXP, handler, ref delay);
                                            break;
                                        case EnchantingProfession:
                                            ProfessionSystem.SetProfession(inventoryConnection.InventoryOwner, user.LocalCharacter.GetEntityOnServer(), steamId, professionXP, handler, ref delay);
                                            EquipmentQualityManager.ApplyPlayerEquipmentStats(steamId, itemEntity);
                                            break;
                                        case TailoringProfession:
                                            ProfessionSystem.SetProfession(inventoryConnection.InventoryOwner, user.LocalCharacter.GetEntityOnServer(), steamId, professionXP, handler, ref delay);
                                            EquipmentQualityManager.ApplyPlayerEquipmentStats(steamId, itemEntity);
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

    static readonly ConcurrentDictionary<NetworkId, DateTime> _stationCooldowns = [];
    const float PRIMAL_COOLDOWN = 0.25f;
    static void SpawnPrimalJewel(Entity station, Entity inventory)
    {
        if (!station.TryGetComponent(out NetworkId networkId))
            return;

        DateTime now = DateTime.UtcNow;

        if (_stationCooldowns.TryGetValue(networkId, out DateTime lastUse) && (now - lastUse).TotalSeconds < PRIMAL_COOLDOWN)
        {
            // Core.Log.LogWarning($"SpawnPrimalJewel - {networkId} is on cooldown, last use was at {lastUse}, now is {now}");
            return;
        }
        else
        {
            _stationCooldowns[networkId] = now;
            ResetStation(networkId).Run();
        }

        PrefabGUID perfectGem = PrefabGUID.Empty;
        PrefabGUID primalJewel = PrefabGUID.Empty;

        foreach (PrefabGUID item in _perfectGems)
        {
            int amount = ServerGameManager.GetInventoryItemCount(station, item);

            if (amount > 0)
            {
                perfectGem = item;
                break;
            }
        }

        if (_perfectGemPrimals.TryGetValue(perfectGem, out PrefabGUID primalJewelTemplate) && ServerGameManager.TryRemoveInventoryItem(station, perfectGem, 1))
        {
            primalJewel = primalJewelTemplate;
        }

        if (ServerGameManager.TryRemoveInventoryItem(inventory, _itemJewelTemplate, 1))
        {
            if (!primalJewel.HasValue()) primalJewel = _jewelTemplates.ElementAt(_random.Next(_jewelTemplates.Count));

            AddItemResponse addResponse = ServerGameManager.TryAddInventoryItem(inventory, primalJewel, 1);
            if (addResponse.Success && addResponse.NewEntity.TryGetComponent(out JewelInstance jewelInstance))
            {
                Entity jewelEntity = addResponse.NewEntity;

                JewelSpawnSystem.UninitializedJewelAbility uninitializedJewel = new()
                {
                    AbilityGuid =  PrefabGUID.Empty,
                    JewelEntity = jewelEntity,
                    JewelTier = jewelInstance.TierIndex
                };

                JewelSpawnSystem.InitializeSpawnedJewel(uninitializedJewel, false); // no idea why the tooltip only shows up right away when this is outside the try block compared to only after closing-opening gem cutter inventory if using try block but moving on x_x
            }
        }
    }
    static IEnumerator ResetStation(NetworkId networkId)
    {
        yield return _delay;

        _stationCooldowns.TryRemove(networkId, out _);
    }
}
