using Bloodcraft.Interfaces;
using Bloodcraft.Resources;
using Bloodcraft.Services;
using ProjectM;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using System.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static Bloodcraft.Utilities.Misc.PlayerBoolsManager;
using static Bloodcraft.Utilities.Progression;
using Random = System.Random;
using User = ProjectM.Network.User;

namespace Bloodcraft.Systems.Professions;
internal static class ProfessionSystem
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static PrefabCollectionSystem PrefabCollectionSystem => SystemService.PrefabCollectionSystem;
    static EndSimulationEntityCommandBufferSystem EndSimulationEntityCommandBufferSystem => SystemService.EndSimulationEntityCommandBufferSystem;

    static readonly Random _random = new();

    const float SCT_DELAY = 0.75f; // trying 1f instead of 0.75f actually 0.5f then add more manually
    const float SCT_DELAY_ADD = 1f;

    const int FISH_STEP = 20;
    const int GREASE_STEP = 4;

    static readonly float _professionMultiplier = ConfigService.ProfessionFactor;
    public const int MAX_PROFESSION_LEVEL = 100;

    static readonly AssetGuid _experienceAssetGuid = AssetGuid.FromString("4210316d-23d4-4274-96f5-d6f0944bd0bb");
    static readonly AssetGuid _bonusYieldAssetGuid = AssetGuid.FromString("5a8b7a32-c3e3-4794-bd62-ace36c10e89e");
    // static readonly AssetGuid _yieldAssetGuid = AssetGuid.FromString("5a8b7a32-c3e3-4794-bd62-ace36c10e89e");

    static readonly PrefabGUID _experienceGainSCT = new(1876501183); // resource gain
    static readonly PrefabGUID _bonusYieldSCT = new(106212079);
    static readonly float3 _bonusYieldColor = new(0.6f, 0.8f, 1f);
    static readonly float3 _mutantGreaseColor = new(0.8f, 1f, 0.1f);
    static readonly float3 _goldOreColor = new(1f, 0.8f, 0f);
    static readonly float3 _seedColor = new(0.6f, 0.9f, 0.6f);
    static readonly float3 _saplingColor = new(0.4f, 0.25f, 0.2f);
    static readonly float3 _radiantFiberColor = new(0.8f, 0.1f, 0.5f);

    static readonly PrefabGUID _goldOre = PrefabGUIDs.Item_Ingredient_Mineral_GoldOre;
    static readonly PrefabGUID _radiantFibre = PrefabGUIDs.Item_Ingredient_Plant_RadiantFiber;
    static readonly PrefabGUID _mutantGrease = PrefabGUIDs.Item_Ingredient_MutantGrease;

    static readonly List<PrefabGUID> _plantSeeds =
    [
        new(-1463158090), // Item_Building_Plants_BleedingHeart_Seed
        new(531984050),   // Item_Building_Plants_BloodRose_Seed
        new(-1289010178), // Item_Building_Plants_Cotton_Seed
        new(675013523),   // Item_Building_Plants_FireBlossom_Seed
        new(1762839393),  // Item_Building_Plants_GhostShroom_Seed
        new(-1681104075), // Item_Building_Plants_Grapes_Seed
        new(-1987586694), // Item_Building_Plants_HellsClarion_Seed
        new(-1495639636), // Item_Building_Plants_Lotus_Seed
        new(-1386314668), // Item_Building_Plants_MourningLily_Seed
        new(1985892973),  // Item_Building_Plants_SnowFlower_Seed
        new(-473351958),  // Item_Building_Plants_Sunflower_Seed
        new(-1370210913), // Item_Building_Plants_Thistle_Seed
        new(1915695899),  // Item_Building_Plants_TrippyShroom_Seed
    ];

    static readonly List<PrefabGUID> _treeSaplings =
    [
        new(-1897495615), // Item_Building_Sapling_AppleCursed_Seed
        new(1226559814),  // Item_Building_Sapling_AppleTree_Seed
        new(1996361886),  // Item_Building_Sapling_Aspen_Seed
        new(-2035190786), // Item_Building_Sapling_AspenAutum_Seed
        new(1552240197),  // Item_Building_Sapling_Birch_Seed
        new(2000981302),  // Item_Building_Sapling_BirchAutum_Seed
        new(-1043479168), // Item_Building_Sapling_Cypress_Seed
        new(-1800289670), // Item_Building_Sapling_GloomTree_Seed
    ];
    public static void UpdateProfessions(Entity playerCharacter, Entity target)
    {
        Entity userEntity = playerCharacter.GetUserEntity();
        User user = userEntity.GetUser();

        ulong steamId = user.PlatformId;

        PrefabGUID itemPrefabGuid = PrefabGUID.Empty;
        if (target.Has<YieldResourcesOnDamageTaken>() && target.Has<EntityCategory>())
        {
            var yield = target.ReadBuffer<YieldResourcesOnDamageTaken>();

            if (yield.IsCreated && !yield.IsEmpty)
            {
                itemPrefabGuid = yield[0].ItemType;
            }
        }
        else
        {
            return;
        }

        float professionValue = target.TryGetComponent(out EntityCategory entityCategory) ? entityCategory.ResourceLevel._Value : 0f;
        PrefabGUID targetPrefabGuid = target.GetPrefabGuid();

        if (!PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(itemPrefabGuid, out Entity prefabEntity)) return;

        if (prefabEntity.TryGetComponent(out entityCategory) && entityCategory.ResourceLevel._Value > professionValue)
        {
            professionValue = prefabEntity.Read<EntityCategory>().ResourceLevel._Value;
        }

        if (target.GetUnitLevel() > professionValue && !targetPrefabGuid.GetPrefabName().Contains("iron", StringComparison.CurrentCultureIgnoreCase))
        {
            professionValue = target.Read<UnitLevel>().Level;
        }

        if (professionValue.Equals(0))
        {
            professionValue = 10;
        }

        // professionValue = (int)(professionValue * _professionMultiplier);
        IProfession handler = ProfessionFactory.GetProfession(itemPrefabGuid);

        if (handler != null)
        {
            Profession profession = handler.GetProfessionEnum();

            if (profession.IsDisabled()) return;
            else if (profession.Equals(Profession.Woodcutting))
            {
                professionValue *= ProfessionMappings.GetWoodcuttingModifier(itemPrefabGuid);
                professionValue *= 10;
            }
            else if (profession.Equals(Profession.Mining))
            {
                professionValue *= 10;
            }

            /*
            string professionName = handler.GetProfessionName();
            if (professionName.Contains("Woodcutting"))
            {
                professionValue *= ProfessionMappings.GetWoodcuttingModifier(itemPrefabGuid);
                professionValue *= 10;
            }
            else if (professionName.Contains("Mining"))
            {
                professionValue *= 10;
            }
            */

            float delay = SCT_DELAY;

            SetProfession(target, playerCharacter, steamId, professionValue, handler, ref delay);
            GiveProfessionBonus(target, targetPrefabGuid, playerCharacter, userEntity, user, steamId, handler, delay);
        }
    }
    public static void GiveProfessionBonus(Entity target, PrefabGUID prefabGuid, Entity playerCharacter, Entity userEntity, User user, ulong steamId, IProfession handler, float delay)
    {
        if (!PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(prefabGuid, out Entity prefabEntity)) return;

        int level = GetLevel(steamId, handler);
        string professionName = handler.GetProfessionName();

        bool professionLogging = GetPlayerBool(steamId, PROFESSION_LOG_KEY);
        bool sctYield = GetPlayerBool(steamId, SCT_YIELD_KEY);

        if (professionName.Contains("Fishing"))
        {
            int bonusYield = level / FISH_STEP;
            int mutantGrease = level / GREASE_STEP;

            if (bonusYield <= 0 && mutantGrease <= 0) return;

            List<PrefabGUID> fishDrops = ProfessionMappings.GetFishingAreaDrops(prefabGuid);
            int index = _random.Next(fishDrops.Count);
            PrefabGUID fish = fishDrops[index];

            if (bonusYield > 0)
            {
                if (ServerGameManager.TryAddInventoryItem(playerCharacter, fish, bonusYield))
                {
                    HandleExperienceAndBonusYield(user, userEntity, playerCharacter, target, fish, professionName, bonusYield, professionLogging, sctYield, ref delay);
                }
                else
                {
                    InventoryUtilitiesServer.CreateDropItem(EntityManager, playerCharacter, fish, bonusYield, new Entity());
                    HandleExperienceAndBonusYield(user, userEntity, playerCharacter, target, fish, professionName, bonusYield, professionLogging, sctYield, ref delay);
                }

                HandleMutantGrease(user, userEntity, playerCharacter, target, professionName, mutantGrease, professionLogging, sctYield, ref delay);
            }
            else
            {
                HandleMutantGrease(user, userEntity, playerCharacter, target, professionName, mutantGrease, professionLogging, sctYield, ref delay);
            }
        }
        else if (prefabEntity.Has<DropTableBuffer>())
        {
            var dropTableBuffer = prefabEntity.ReadBuffer<DropTableBuffer>();

            foreach (var drop in dropTableBuffer)
            {
                switch (drop.DropTrigger)
                {
                    case DropTriggerType.YieldResourceOnDamageTaken:
                        Entity dropTable = PrefabCollectionSystem._PrefabGuidToEntityMap[drop.DropTableGuid];
                        var dropTableDataBuffer = dropTable.ReadBuffer<DropTableDataBuffer>();

                        foreach (var dropTableData in dropTableDataBuffer)
                        {
                            string itemName = dropTableData.ItemGuid.GetPrefabName();
                            // string localizedItemName = dropTableData.ItemGuid.GetLocalizedName();

                            if (itemName.Contains("ingredient", StringComparison.CurrentCultureIgnoreCase) || itemName.Contains("trippyshroom", StringComparison.CurrentCultureIgnoreCase))
                            {
                                int bonusYield;

                                if (itemName.Contains("plant", StringComparison.CurrentCultureIgnoreCase) || itemName.Contains("trippyshroom", StringComparison.CurrentCultureIgnoreCase))
                                {
                                    bonusYield = level / 10;
                                }
                                else
                                {
                                    bonusYield = level / 2;
                                }

                                if (bonusYield <= 0) return;
                                else if (ServerGameManager.TryAddInventoryItem(playerCharacter, dropTableData.ItemGuid, bonusYield))
                                {
                                    HandleExperienceAndBonusYield(user, userEntity, playerCharacter, target, dropTableData.ItemGuid, professionName, bonusYield, professionLogging, sctYield, ref delay);

                                    if (professionName.Contains("Mining"))
                                    {
                                        int goldOre = GoldOreRoll(level);
                                        if (goldOre > 0) HandleGoldOre(user, userEntity, playerCharacter, target, professionName, goldOre, professionLogging, sctYield, ref delay);
                                    }
                                    else if (professionName.Contains("Harvesting"))
                                    {
                                        int bonusAmount = RadiantFiberRoll(level);
                                        if (bonusAmount > 0) HandleRadiantFiber(user, userEntity, playerCharacter, target, professionName, bonusAmount, professionLogging, sctYield, ref delay);
                                    }
                                    else if (professionName.Contains("Woodcutting"))
                                    {
                                        List<PrefabGUID> bonusSaplings = TreeSaplingsRoll(level);
                                        if (bonusSaplings.Any()) HandleTreeSaplings(user, userEntity, playerCharacter, target, professionName, bonusSaplings, professionLogging, sctYield, ref delay);
                                    }

                                    break;
                                }
                                else
                                {
                                    InventoryUtilitiesServer.CreateDropItem(EntityManager, playerCharacter, dropTableData.ItemGuid, bonusYield, new Entity());
                                    HandleExperienceAndBonusYield(user, userEntity, playerCharacter, target, dropTableData.ItemGuid, professionName, bonusYield, professionLogging, sctYield, ref delay);

                                    if (professionName.Contains("Mining"))
                                    {
                                        int goldOre = GoldOreRoll(level);

                                        if (goldOre > 0)
                                        {
                                            HandleGoldOre(user, userEntity, playerCharacter, target, professionName,goldOre, professionLogging, sctYield, ref delay);
                                        }
                                    }
                                    else if (professionName.Contains("Harvesting"))
                                    {
                                        int bonusAmount = RadiantFiberRoll(level);
                                        if (bonusAmount > 0) HandleRadiantFiber(user, userEntity, playerCharacter, target, professionName, bonusAmount, professionLogging, sctYield, ref delay);
                                    }
                                    else if (professionName.Contains("Woodcutting"))
                                    {
                                        List<PrefabGUID> bonusSaplings = TreeSaplingsRoll(level);
                                        HandleTreeSaplings(user, userEntity, playerCharacter, target, professionName, bonusSaplings, professionLogging, sctYield, ref delay);
                                    }

                                    break;
                                }
                            }
                        }
                        break;
                    /*
                    case DropTriggerType.OnDeath:
                        WIP
                        dropTable = prefabCollectionSystem._PrefabGuidToEntityMap[drop.DropTableGuid];
                        dropTableDataBuffer = dropTable.ReadBuffer<DropTableDataBuffer>();
                        foreach (var dropTableData in dropTableDataBuffer)
                        {
                            prefabEntity = prefabCollectionSystem._PrefabGuidToEntityMap[dropTableData.ItemGuid];
                            if (!prefabEntity.Has<ItemDataDropGroupBuffer>()) continue;
                            var itemDataDropGroupBuffer = prefabEntity.ReadBuffer<ItemDataDropGroupBuffer>();
                            foreach (var itemDataDropGroup in itemDataDropGroupBuffer)
                            {
                                Core.Log.LogInfo($"{itemDataDropGroup.DropItemPrefab.GetPrefabName()} | {itemDataDropGroup.Quantity} | {itemDataDropGroup.Weight}");
                            }
                        }
                        break;
                    */
                    default:
                        break;
                }
            }
        }
    }
    public static void SetProfession(Entity target, Entity source, ulong steamID, float value, IProfession handler, ref float delay)
    {
        value *= _professionMultiplier;
        var xpData = handler.GetProfessionData(steamID);

        if (xpData.Key >= MAX_PROFESSION_LEVEL) return;

        UpdateProfessionExperience(target, source, steamID, xpData, value, handler, ref delay);
    }
    static void UpdateProfessionExperience(Entity target, Entity source, ulong steamId, KeyValuePair<int, float> xpData, float gainedXP, IProfession handler, ref float delay)
    {
        float newExperience = xpData.Value + gainedXP;
        int newLevel = ConvertXpToLevel(newExperience);
        bool leveledUp = false;

        if (newLevel > xpData.Key)
        {
            leveledUp = true;
            if (newLevel > MAX_PROFESSION_LEVEL)
            {
                newLevel = MAX_PROFESSION_LEVEL;
                newExperience = ConvertLevelToXp(MAX_PROFESSION_LEVEL);
            }
        }

        var updatedXPData = new KeyValuePair<int, float>(newLevel, newExperience);
        handler.SetProfessionData(steamId, updatedXPData);

        NotifyPlayer(target, source, steamId, gainedXP, leveledUp, handler, ref delay);
    }
    static void NotifyPlayer(Entity target, Entity playerCharacter, ulong steamID, float gainedXP, bool leveledUp, IProfession handler, ref float delay)
    {
        Entity userEntity = playerCharacter.GetUserEntity();
        User user = userEntity.GetUser();

        string professionName = handler.GetProfessionName();

        if (leveledUp)
        {
            int newLevel = ConvertXpToLevel(handler.GetProfessionData(steamID).Value);
            if (newLevel < MAX_PROFESSION_LEVEL) LocalizationService.HandleServerReply(EntityManager, user, $"{professionName} improved to [<color=white>{newLevel}</color>]");
        }

        if (GetPlayerBool(steamID, PROFESSION_LOG_KEY))
        {
            int levelProgress = GetLevelProgress(steamID, handler);
            LocalizationService.HandleServerReply(EntityManager, user, $"+<color=yellow>{(int)gainedXP}</color> <color=#FFC0CB>proficiency</color> in {professionName.ToLower()} (<color=white>{levelProgress}%</color>)");
        }

        if (GetPlayerBool(steamID, SCT_PROFESSIONS_KEY))
        {
            float3 targetPosition = target.GetPosition();
            float3 professionColor = handler.GetProfessionColor();

            ProfessionSCTDelayRoutine(_experienceGainSCT, _experienceAssetGuid, playerCharacter, userEntity, targetPosition, professionColor, gainedXP, delay).Run();
        }
    }
    static IEnumerator ProfessionSCTDelayRoutine(PrefabGUID sctPrefabGuid, AssetGuid assetGuid, Entity playerCharacter, Entity userEntity, float3 position, float3 color, float value, float delay)
    {
        yield return new WaitForSeconds(delay);

        try
        {
            ScrollingCombatTextMessage.Create(EntityManager, EndSimulationEntityCommandBufferSystem.CreateCommandBuffer(), assetGuid, position, color, playerCharacter, value, sctPrefabGuid, userEntity);
        }
        catch (Exception e)
        {
            Core.Log.LogError($"Error in ProfessionSCTDelayRoutine: {e.Message}");
        }
    }
    static float GetXp(ulong steamID, IProfession handler)
    {
        var xpData = handler.GetProfessionData(steamID);
        return xpData.Value;
    }
    static int GetLevel(ulong steamID, IProfession handler)
    {
        return ConvertXpToLevel(GetXp(steamID, handler));
    }
    public static int GetLevelProgress(ulong steamID, IProfession handler)
    {
        float currentXP = GetXp(steamID, handler);
        int currentLevelXP = ConvertLevelToXp(GetLevel(steamID, handler));
        int nextLevelXP = ConvertLevelToXp(GetLevel(steamID, handler) + 1);

        double neededXP = nextLevelXP - currentLevelXP;
        double earnedXP = nextLevelXP - currentXP;
        return 100 - (int)Math.Ceiling(earnedXP / neededXP * 100);
    }
    static void HandleBonusYieldScrollingText(Entity target, PrefabGUID sctPrefabGuid, AssetGuid assetGuid, Entity playerCharacter, Entity userEntity, float3 color, float bonusYield, ref float delay)
    {
        float3 targetPosition = target.GetPosition();

        ProfessionSCTDelayRoutine(sctPrefabGuid, assetGuid, playerCharacter, userEntity, targetPosition, color, bonusYield, delay).Run();
        delay += SCT_DELAY_ADD;
    }
    static void HandleExperienceAndBonusYield(User user, Entity userEntity, Entity playerCharacter, Entity target, PrefabGUID resource, string professionName, float bonusYield, bool professionLogging, bool sctYield, ref float delay)
    {
        if (professionLogging) LocalizationService.HandleServerReply(EntityManager, user, $"<color=green>{resource.GetLocalizedName()}</color>x<color=white>{bonusYield}</color> received from {professionName}");
        if (sctYield) HandleBonusYieldScrollingText(target, _bonusYieldSCT, _bonusYieldAssetGuid, playerCharacter, userEntity, _bonusYieldColor, bonusYield, ref delay);
    }
    static int GoldOreRoll(int level)
    {
        if (level < 20) return 0;

        int maxRolls = Math.Min(5, (level - 20) / 20 + 1);
        int goldOreCount = 0;

        int[] successChances = [10, 20, 30, 40, 50];

        for (int i = 0; i < maxRolls; i++)
        {
            int roll = _random.Next(1, 101);
            if (roll <= successChances[i])
            {
                goldOreCount++;
            }
        }

        return goldOreCount;
    }
    static void HandleGoldOre(User user, Entity userEntity, Entity playerCharacter, Entity target, string professionName, int goldOre, bool professionLogging, bool sctYield, ref float delay)
    {
        if (ServerGameManager.TryAddInventoryItem(playerCharacter, _goldOre, goldOre))
        {
            if (professionLogging) LocalizationService.HandleServerReply(EntityManager, user, $"<color=green>Gold Ore</color>x<color=white>{goldOre}</color> received from {professionName}");
            if (sctYield) HandleBonusYieldScrollingText(target, _bonusYieldSCT, _bonusYieldAssetGuid, playerCharacter, userEntity, _goldOreColor, goldOre, ref delay);
        }
        else
        {
            InventoryUtilitiesServer.CreateDropItem(EntityManager, playerCharacter, _goldOre, goldOre, new Entity());
            if (professionLogging) LocalizationService.HandleServerReply(EntityManager, user, $"<color=green>Gold Ore</color>x<color=white>{goldOre}</color> received from {professionName}, but it dropped on the ground since your inventory is full.");
            if (sctYield) HandleBonusYieldScrollingText(target, _bonusYieldSCT, _bonusYieldAssetGuid, playerCharacter, userEntity, _goldOreColor, goldOre, ref delay);
        }
    }
    static int RadiantFiberRoll(int level)
    {
        if (level < 20) return 0;

        int maxRolls = Math.Min(5, (level - 20) / 20 + 1);
        int radiantFiberCount = 0;

        int[] successChances = [1, 2, 3, 4, 5];

        for (int i = 0; i < maxRolls; i++)
        {
            int roll = _random.Next(1, 101);
            if (roll <= successChances[i])
            {
                radiantFiberCount++;
            }
        }

        return radiantFiberCount;
    }
    static void HandleRadiantFiber(User user, Entity userEntity, Entity playerCharacter, Entity target, string professionName, int amount, bool professionLogging, bool sctYield, ref float delay)
    {
        if (ServerGameManager.TryAddInventoryItem(playerCharacter, _radiantFibre, amount))
        {
            if (professionLogging) LocalizationService.HandleServerReply(EntityManager, user, $"<color=green>Gold Ore</color>x<color=white>{amount}</color> received from {professionName}");
            if (sctYield) HandleBonusYieldScrollingText(target, _bonusYieldSCT, _bonusYieldAssetGuid, playerCharacter, userEntity, _radiantFiberColor, amount, ref delay);
        }
        else
        {
            InventoryUtilitiesServer.CreateDropItem(EntityManager, playerCharacter, _radiantFibre, amount, new Entity());

            if (professionLogging) LocalizationService.HandleServerReply(EntityManager, user, $"<color=green>Gold Ore</color>x<color=white>{amount}</color> received from {professionName}, but it dropped on the ground since your inventory is full.");
            if (sctYield) HandleBonusYieldScrollingText(target, _bonusYieldSCT, _bonusYieldAssetGuid, playerCharacter, userEntity, _radiantFiberColor, amount, ref delay);
        }
    }

    /*
    static List<PrefabGUID> PlantSeedsRoll(int level)
    {
        if (level < 20) return []; // No rolls if below level 20.

        int maxRolls = Math.Min(5, (level - 20) / 20 + 1); // Calculate number of rolls (up to 5).
        List<PrefabGUID> harvestedSeeds = [];
        int[] successChances = [1, 2, 3, 4, 5]; // Success chances per roll.

        for (int i = 0; i < maxRolls; i++)
        {
            int roll = _random.Next(1, 101); // Random number between 1-100.
            if (roll <= successChances[i])
            {
                int index = _random.Next(0, _plantSeeds.Count); // Random seed selection.
                harvestedSeeds.Add(_plantSeeds[index]);
            }
        }

        return harvestedSeeds;
    }
    static void HandlePlantSeeds(User user, Entity userEntity, Entity playerCharacter, Entity target, string professionName, List<PrefabGUID> seeds, bool professionLogging, bool sctYield, ref float delay)
    {
        int quantity = seeds.Count;
        bool fellOnGround = false;

        bool notify = false;

        foreach (PrefabGUID seed in seeds)
        {
            if (ServerGameManager.TryAddInventoryItem(playerCharacter, seed, 1))
            {
                notify = true;
            }
            else
            {
                InventoryUtilitiesServer.CreateDropItem(EntityManager, playerCharacter, seed, 1, new Entity());

                fellOnGround = true;
                notify = true;
            }
        }

        if (notify)
        {
            if (professionLogging)
            {
                if (!fellOnGround)
                {
                    LocalizationService.HandleServerReply(EntityManager, user, $"<color=green>Bonus Seed(s)</color>x<color=white>{quantity}</color> received from {professionName}!");
                }
                else
                {
                    LocalizationService.HandleServerReply(EntityManager, user, $"<color=green>Bonus Seed(s)</color>x<color=white>{quantity}</color> received from {professionName}, but some fell on the ground since your inventory is full.");
                }
            }

            if (sctYield)
            {
                HandleBonusYieldScrollingText(target, _bonusYieldSCT, _bonusYieldAssetGuid, playerCharacter, userEntity, _seedColor, quantity, ref delay);
            }
        }
    }
    */
    static List<PrefabGUID> TreeSaplingsRoll(int level)
    {
        if (level < 20) return []; // No rolls if below level 20.

        int maxRolls = Math.Min(5, (level - 20) / 20 + 1); // Calculate number of rolls (up to 5).
        List<PrefabGUID> harvestedSaplings = [];
        int[] successChances = [4, 8, 12, 16, 20]; // Success chances per roll.

        for (int i = 0; i < maxRolls; i++)
        {
            int roll = _random.Next(1, 101); // Random number between 1-100.
            if (roll <= successChances[i])
            {
                int index = _random.Next(0, _treeSaplings.Count); // Random sapling selection.
                harvestedSaplings.Add(_treeSaplings[index]);
            }
        }

        return harvestedSaplings;
    }
    static void HandleTreeSaplings(User user, Entity userEntity, Entity playerCharacter, Entity target, string professionName, List<PrefabGUID> saplings, bool professionLogging, bool sctYield, ref float delay)
    {
        int quantity = saplings.Count;

        bool fellOnGround = false;
        bool notify = false;

        foreach (PrefabGUID seed in saplings)
        {
            if (ServerGameManager.TryAddInventoryItem(playerCharacter, seed, 1))
            {
                notify = true;
            }
            else
            {
                InventoryUtilitiesServer.CreateDropItem(EntityManager, playerCharacter, seed, 1, new Entity());

                fellOnGround = true;
                notify = true;
            }
        }

        if (notify)
        {
            if (professionLogging)
            {
                if (!fellOnGround)
                {
                    LocalizationService.HandleServerReply(EntityManager, user, $"<color=green>Bonus Saplings(s)</color>x<color=white>{quantity}</color> received from {professionName}!");
                }
                else
                {
                    LocalizationService.HandleServerReply(EntityManager, user, $"<color=green>Bonus Saplings(s)</color>x<color=white>{quantity}</color> received from {professionName}, but some fell on the ground since your inventory is full.");
                }
            }

            if (sctYield)
            {
                HandleBonusYieldScrollingText(target, _bonusYieldSCT, _bonusYieldAssetGuid, playerCharacter, userEntity, _saplingColor, quantity, ref delay);
            }
        }
    }
    static void HandleMutantGrease(User user, Entity userEntity, Entity playerCharacter, Entity target, string professionName, int mutantGrease, bool professionLogging, bool sctYield, ref float delay)
    {
        if (ServerGameManager.TryAddInventoryItem(playerCharacter, _mutantGrease, mutantGrease))
        {
            if (professionLogging) LocalizationService.HandleServerReply(EntityManager, user, $"<color=green>Mutant Grease</color>x<color=white>{mutantGrease}</color> received from {professionName}");

            if (sctYield) HandleBonusYieldScrollingText(target, _bonusYieldSCT, _bonusYieldAssetGuid, playerCharacter, userEntity, _mutantGreaseColor, mutantGrease, ref delay);
        }
        else
        {
            InventoryUtilitiesServer.CreateDropItem(EntityManager, playerCharacter, _mutantGrease, mutantGrease, new Entity());

            if (professionLogging) LocalizationService.HandleServerReply(EntityManager, user, $"<color=green>Mutant Grease</color>x<color=white>{mutantGrease}</color> received from {professionName}, but it dropped on the ground since your inventory is full.");
            if (sctYield) HandleBonusYieldScrollingText(target, _bonusYieldSCT, _bonusYieldAssetGuid, playerCharacter, userEntity, _mutantGreaseColor, mutantGrease, ref delay);
        }
    }
}
internal static class ProfessionMappings
{
    static readonly Dictionary<string, int> _fishingMultipliers = new()
    {
        { "farbane", 2 },
        { "dunley", 2 },
        { "gloomrot", 3 },
        { "cursed", 4 },
        { "silverlight", 5 },
        { "strongblade", 4 }
    };

    static readonly List<PrefabGUID> _farbaneFishDrops = new()
    {
        { new(-1642545082)} //goby
    };

    static readonly List<PrefabGUID> _dunleyFishDrops = new()
    {
        { new(-1642545082) }, //goby
        { new(447901086) }, //stinger
        { new(-149778795) } //rainbow
    };

    static readonly List<PrefabGUID> _gloomrotFishDrops = new()
    {
        { new(-1642545082) }, //goby
        { new(447901086) }, //stinger
        { new(-149778795) }, //rainbow
        { new(736318803) }, //sagefish
        { new(-1779269313) } //bloodsnapper
    };

    static readonly List<PrefabGUID> _cursedFishDrops = new()
    {
        { new(-1642545082) }, //goby
        { new(447901086) }, //stinger
        { new(-149778795) }, //rainbow
        { new(736318803) }, //sagefish
        { new(-1779269313) }, //bloodsnapper
        { new(177845365) } //swampdweller
    };

    static readonly List<PrefabGUID> _silverlightFishDrops = new()
    {
        { new(-1642545082) }, //goby
        { new(447901086) }, //stinger
        { new(-149778795) }, //rainbow
        { new(736318803) }, //sagefish
        { new(-1779269313) }, //bloodsnapper
        { new(67930804) } //goldenbassriver
    };

    static readonly List<PrefabGUID> _oakveilFishDrops = new()
    {
        { new(-1642545082) }, //goby
        { new(447901086) }, //stinger
        { new(-149778795) }, //rainbow
        { new(736318803) }, //sagefish
        { new(-1779269313) }, //bloodsnapper
        { new(67930804) }, //goldenbassriver
        { PrefabGUIDs.Item_Ingredient_Fish_Corrupted_T03 }
    };

    static readonly Dictionary<string, List<PrefabGUID>> _fishingAreaDrops = new()
    {
        { "farbane", _farbaneFishDrops},
        { "dunley", _dunleyFishDrops},
        { "gloomrot", _gloomrotFishDrops},
        { "cursed", _cursedFishDrops},
        { "silverlight", _silverlightFishDrops},
        { "strongblade", _oakveilFishDrops}
    };

    static readonly Dictionary<string, int> _woodcuttingMultipliers = new()
    {
        { "hallow", 2 },
        { "gloom", 3 },
        { "cursed", 4 },
        { "corrupted", 4 }
    };

    static readonly Dictionary<string, int> _tierMultiplier = new()
    {
        { "t01", 1 },
        { "t02", 2 },
        { "t03", 3 },
        { "t04", 4 },
        { "t05", 5 },
        { "t06", 6 },
        { "t07", 7 },
        { "t08", 8 },
        { "t09", 9 },
    };
    public static int GetFishingModifier(PrefabGUID prefab)
    {
        foreach (KeyValuePair<string, int> location in _fishingMultipliers)
        {
            if (prefab.GetPrefabName().ToLower().Contains(location.Key))
            {
                return location.Value;
            }
        }
        return 1;
    }
    public static List<PrefabGUID> GetFishingAreaDrops(PrefabGUID prefab)
    {
        foreach (KeyValuePair<string, List<PrefabGUID>> location in _fishingAreaDrops)
        {
            if (prefab.GetPrefabName().ToLower().Contains(location.Key))
            {
                return location.Value;
            }
            else if (prefab.GetPrefabName().Contains("general", StringComparison.CurrentCultureIgnoreCase))
            {
                return _farbaneFishDrops;
            }
        }
        throw new InvalidOperationException("Unrecognized fishing area");
    }
    public static int GetWoodcuttingModifier(PrefabGUID prefab)
    {
        foreach (KeyValuePair<string, int> location in _woodcuttingMultipliers)
        {
            if (prefab.GetPrefabName().ToLower().Contains(location.Key))
            {
                return location.Value;
            }
        }

        return 1;
    }
    public static int GetTierMultiplier(PrefabGUID prefab)
    {
        foreach (KeyValuePair<string, int> tier in _tierMultiplier)
        {
            if (prefab.GetPrefabName().ToLower().Contains(tier.Key))
            {
                return tier.Value;
            }
        }

        return 1;
    }
    public static bool IsDisabled(this Profession profession)
    {
        return Core.DisabledProfessions.Contains(profession);
    }
}
