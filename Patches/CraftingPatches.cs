using Bloodcraft.Systems.Professions;
using Bloodcraft.SystemUtilities.Quests;
using HarmonyLib;
using ProjectM;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using User = ProjectM.Network.User;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class CraftingPatches
{
    static readonly float RecipeDurationMultiplier = Core.ServerGameSettingsSystem._Settings.CraftRateModifier;
    const float CraftThreshold = 0.995f;
    static Dictionary<Entity, Dictionary<PrefabGUID, DateTime>> CraftCooldowns = [];
    /*
    [HarmonyPatch(typeof(StartCharacterCraftingSystem), nameof(StartCharacterCraftingSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(StartCharacterCraftingSystem __instance)
    {
        NativeArray<Entity> entities = __instance._StartCharacterCraftItemEventQuery.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (Plugin.ProfessionSystem.Value)
                {
                    if (!Core.hasInitialized) continue;

                    if (entity.Has<StartCharacterCraftItemEvent>() && entity.Has<FromCharacter>())
                    {
                        FromCharacter fromCharacter = entity.Read<FromCharacter>();
                        ulong steamId = fromCharacter.User.Read<User>().PlatformId;
                        StartCharacterCraftItemEvent startCraftItemEvent = entity.Read<StartCharacterCraftItemEvent>();
                        PrefabGUID PrefabGUID = startCraftItemEvent.RecipeId;
                        Entity recipeEntity = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[PrefabGUID];
                        var buffer = recipeEntity.ReadBuffer<RecipeOutputBuffer>();
                        PrefabGUID itemPrefab = buffer[0].Guid;
                        // Ensure the player’s job list exists
                        if (!Core.DataStructures.PlayerCraftingJobs.TryGetValue(steamId, out var playerJobs))
                        {
                            playerJobs = [];
                            Core.DataStructures.PlayerCraftingJobs.Add(steamId, playerJobs);
                        }

                        // Check if the job exists and update or add
                        var jobExists = false;
                        for (int i = 0; i < playerJobs.Count; i++)
                        {
                            if (playerJobs[i].Item1.Equals(itemPrefab))
                            {
                                //Core.Log.LogInfo($"Adding Craft to existing: {itemPrefab.GetPrefabName()}");
                                playerJobs[i] = (playerJobs[i].Item1, playerJobs[i].Item2 + 1);
                                jobExists = true;
                                break;
                            }
                        }
                        if (!jobExists)
                        {
                            //Core.Log.LogInfo($"Adding Craft: {itemPrefab.GetPrefabName()}");
                            playerJobs.Add((itemPrefab, 1));
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Core.Log.LogInfo($"Exited StartCraftingSystem hook early: {e}");
        }
        finally
        {
            entities.Dispose();
        }
    }

    [HarmonyPatch(typeof(StopCharacterCraftingSystem), nameof(StopCharacterCraftingSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(StopCharacterCraftingSystem __instance)
    {
        NativeArray<Entity> entities = __instance._StopCharacterCraftItemEventQuery.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (Plugin.ProfessionSystem.Value)
                {
                    if (!Core.hasInitialized) continue;

                    if (entity.Has<StopCharacterCraftItemEvent>() && entity.Has<FromCharacter>())
                    {                        
                        FromCharacter fromCharacter = entity.Read<FromCharacter>();
                        ulong steamId = fromCharacter.User.Read<User>().PlatformId;
                        StopCharacterCraftItemEvent startCraftItemEvent = entity.Read<StopCharacterCraftItemEvent>();
                        PrefabGUID PrefabGUID = startCraftItemEvent.RecipeGuid;
                        Entity recipeEntity = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[PrefabGUID];
                        var buffer = recipeEntity.ReadBuffer<RecipeOutputBuffer>();
                        PrefabGUID itemPrefab = buffer[0].Guid;
                        if (Core.DataStructures.PlayerCraftingJobs.TryGetValue(steamId, out var playerJobs))
                        {
                            // if crafting job is active, remove
                            for (int i = 0; i < playerJobs.Count; i++)
                            {
                                if (playerJobs[i].Item1 == itemPrefab && playerJobs[i].Item2 > 0)
                                {
                                    //Core.Log.LogInfo($"Removing Craft: {itemPrefab.GetPrefabName()}");
                                    playerJobs[i] = (playerJobs[i].Item1, playerJobs[i].Item2 - 1);
                                    if (playerJobs[i].Item2 == 0) playerJobs.RemoveAt(i);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Core.Log.LogInfo($"Exited StartCraftingSystem hook early: {e}");
        }
        finally
        {
            entities.Dispose();
        }
    }

    [HarmonyPatch(typeof(StartCraftingSystem), nameof(StartCraftingSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(StartCraftingSystem __instance)
    {
        NativeArray<Entity> entities = __instance._StartCraftItemEventQuery.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (Plugin.ProfessionSystem.Value)
                {
                    if (!Core.hasInitialized) continue;

                    if (entity.Has<StartCraftItemEvent>() && entity.Has<FromCharacter>())
                    {
                        FromCharacter fromCharacter = entity.Read<FromCharacter>();
                        ulong steamId = fromCharacter.User.Read<User>().PlatformId;
                        StartCraftItemEvent startCraftItemEvent = entity.Read<StartCraftItemEvent>();
                        PrefabGUID PrefabGUID = startCraftItemEvent.RecipeId;
                        Entity recipeEntity = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[PrefabGUID];
                        var buffer = recipeEntity.ReadBuffer<RecipeOutputBuffer>();
                        PrefabGUID itemPrefab = buffer[0].Guid;
                                                // Ensure the player’s job list exists
                        if (!Core.DataStructures.PlayerCraftingJobs.TryGetValue(steamId, out var playerJobs))
                        {
                            playerJobs = [];
                            Core.DataStructures.PlayerCraftingJobs.Add(steamId, playerJobs);
                        }

                        // Check if the job exists and update or add
                        var jobExists = false;
                        for (int i = 0; i < playerJobs.Count; i++)
                        {
                            if (playerJobs[i].Item1.Equals(itemPrefab))
                            {
                                //Core.Log.LogInfo($"Adding Craft to existing: {itemPrefab.GetPrefabName()}");
                                playerJobs[i] = (playerJobs[i].Item1, playerJobs[i].Item2 + 1);
                                jobExists = true;
                                break;
                            }
                        }
                        if (!jobExists)
                        {
                            //Core.Log.LogInfo($"Adding Craft: {itemPrefab.GetPrefabName()}");
                            playerJobs.Add((itemPrefab, 1));
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Core.Log.LogInfo($"Exited StartCraftingSystem hook early: {e}");
        }
        finally
        {
            entities.Dispose();
        }
    }

    [HarmonyPatch(typeof(StopCraftingSystem), nameof(StopCraftingSystem.OnUpdate))]
    [HarmonyPrefix]
    static void Prefix(StopCraftingSystem __instance)
    {
        //Core.Log.LogInfo("StopCraftingSystemPrefix called...");

        NativeArray<Entity> entities = __instance._EventQuery.ToEntityArray(Allocator.Temp);// double check this
        try
        {
            foreach (Entity entity in entities)
            {
                if (!Core.hasInitialized) continue;

                if (Plugin.ProfessionSystem.Value)
                {
                    if (entity.Has<StopCraftItemEvent>() && entity.Has<FromCharacter>())
                    {
                        FromCharacter fromCharacter = entity.Read<FromCharacter>();
                        ulong steamId = fromCharacter.User.Read<User>().PlatformId;
                        StopCraftItemEvent stopCraftItemEvent = entity.Read<StopCraftItemEvent>();
                        PrefabGUID PrefabGUID = stopCraftItemEvent.RecipeGuid;
                        Entity recipeEntity = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[PrefabGUID];
                        var buffer = recipeEntity.ReadBuffer<RecipeOutputBuffer>();
                        PrefabGUID itemPrefab = buffer[0].Guid;
                        if (Core.DataStructures.PlayerCraftingJobs.TryGetValue(steamId, out var playerJobs))
                        {
                            // if crafting job is active, remove
                            for (int i = 0; i < playerJobs.Count; i++)
                            {
                                if (playerJobs[i].Item1 == itemPrefab && playerJobs[i].Item2 > 0)
                                {
                                    //Core.Log.LogInfo($"Removing Craft: {itemPrefab.GetPrefabName()}");
                                    playerJobs[i] = (playerJobs[i].Item1, playerJobs[i].Item2 - 1);
                                    if (playerJobs[i].Item2 == 0) playerJobs.RemoveAt(i);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Core.Log.LogInfo($"Exited StopCraftingSystem hook early: {e}");
        }
        finally
        {
            entities.Dispose();
        }
    }
    */

    [HarmonyPatch(typeof(ForgeSystem_Update), nameof(ForgeSystem_Update.OnUpdate))]
    static class ForgeSystem_UpdatesPatch
    {
        public static void Prefix(ForgeSystem_Update __instance)
        {
            var repairEntities = __instance.__query_1536473549_0.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (Entity entity in repairEntities)
                {
                    if (!Core.hasInitialized) continue;
                    if (!Plugin.ProfessionSystem.Value) continue;

                    Forge_Shared forge_Shared = entity.Read<Forge_Shared>();
                    if (forge_Shared.State == ForgeState.Empty) continue;

                    UserOwner userOwner = entity.Read<UserOwner>();
                    Entity userEntity = userOwner.Owner._Entity;
                    User user = userEntity.Read<User>();
                    ulong steamId = user.PlatformId;

                    Entity itemEntity = forge_Shared.ItemEntity._Entity;
                    PrefabGUID itemPrefab = new(0);

                    if (itemEntity.Has<ShatteredItem>())
                    {
                        itemPrefab = itemEntity.Read<ShatteredItem>().OutputItem;
                    }
                    else if (itemEntity.Has<UpgradeableLegendaryItem>())
                    {
                        int tier = itemEntity.Read<UpgradeableLegendaryItem>().CurrentTier;
                        var buffer = itemEntity.ReadBuffer<UpgradeableLegendaryItemTiers>();
                        itemPrefab = buffer[tier].TierPrefab;
                    }

                    if (itemPrefab.GuidHash == 0) continue;

                    if (forge_Shared.State == ForgeState.Finished)
                    {
                        //Core.Log.LogInfo($"Forge finished: {itemPrefab.LookupName()}");
                        float ProfessionValue = 50f;
                        ProfessionValue *= ProfessionMappings.GetTierMultiplier(itemPrefab);
                        IProfessionHandler handler = ProfessionHandlerFactory.GetProfessionHandler(itemPrefab, "");
                        if (handler != null)
                        {
                            if (itemEntity.Has<Durability>())
                            {
                                Entity originalItem = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[itemPrefab];
                                Durability durability = itemEntity.Read<Durability>();
                                Durability originalDurability = originalItem.Read<Durability>();

                                if (durability.MaxDurability != originalDurability.MaxDurability) continue; // already handled

                                int level = handler.GetExperienceData(steamId).Key;
                                durability.MaxDurability *= (1 + (float)level / (float)Plugin.MaxProfessionLevel.Value);
                                durability.Value = durability.MaxDurability;
                                itemEntity.Write(durability);
                                ProfessionUtilities.SetProfession(user, steamId, ProfessionValue, handler);
                            }
                        }
                    }
                }
            }
            finally
            {
                repairEntities.Dispose();
            }
        }
    }

    [HarmonyPatch(typeof(UpdateCraftingSystem), nameof(UpdateCraftingSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(UpdateCraftingSystem __instance)
    {
        NativeArray<Entity> entities = __instance.__query_1831452865_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!Core.hasInitialized) continue;
                if (!Plugin.ProfessionSystem.Value) continue;

                if (entity.Has<CastleWorkstation>() && entity.Has<QueuedWorkstationCraftAction>())
                {
                    var buffer = entity.ReadBuffer<QueuedWorkstationCraftAction>();
                    double recipeReduction = entity.Read<CastleWorkstation>().WorkstationLevel.HasFlag(WorkstationLevel.MatchingFloor) ? 0.75 : 1;
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        var item = buffer[i];
                        Entity userEntity = item.InitiateUser;
                        User user = userEntity.Read<User>();
                        ulong steamId = user.PlatformId;
                        PrefabGUID recipePrefab = item.RecipeGuid;
                        Entity recipeEntity = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[recipePrefab];
                        double totalTime = recipeEntity.Read<RecipeData>().CraftDuration * recipeReduction;
                        if (RecipeDurationMultiplier != 1f)
                        {
                            totalTime *= 1 - (RecipeDurationMultiplier - 1);
                        }
                        float progress = item.ProgressTime;
                        //Core.Log.LogInfo($"Progress: {progress}, Total Time: {totalTime} ({progress / totalTime}%) | {RecipeDurationMultiplier}");
                        if (progress / (float)totalTime >= CraftThreshold)
                        {
                            DateTime now = DateTime.Now;
                            if (CraftCooldowns.TryGetValue(userEntity, out var cooldowns))
                            {
                                if (cooldowns.TryGetValue(recipePrefab, out var lastCrafted))
                                {
                                    if ((now - lastCrafted).TotalSeconds < 5)
                                    {
                                        //Core.Log.LogInfo($"Recipe {recipePrefab.LookupName()} on cooldown for {(now - lastCrafted).TotalSeconds} more seconds...");
                                        continue;
                                    }
                                }
                                else
                                {
                                    cooldowns.Add(recipePrefab, now);
                                }
                            }
                            else
                            {
                                //Core.Log.LogInfo($"Adding stamp for {recipePrefab.LookupName()} at: {now}");
                                CraftCooldowns.Add(userEntity, new Dictionary<PrefabGUID, DateTime> { { recipePrefab, now } });
                            }

                            var outputBuffer = recipeEntity.ReadBuffer<RecipeOutputBuffer>();
                            PrefabGUID itemPrefab = outputBuffer[0].Guid;

                            if (Core.DataStructures.PlayerQuests.TryGetValue(steamId, out var questData))
                            {
                                QuestUtilities.UpdateQuestProgress(questData, itemPrefab, 1, user);
                            }

                            if (!Core.DataStructures.PlayerCraftingJobs.TryGetValue(userEntity, out var playerJobs))
                            {
                                playerJobs = [];
                                Core.DataStructures.PlayerCraftingJobs.Add(userEntity, playerJobs);
                            }

                            if (playerJobs.TryGetValue(itemPrefab, out var recipeJobs))
                            {
                                //Core.Log.LogInfo("updatecraftpatch" + itemPrefab.LookupName());
                                recipeJobs++;
                            }
                            else
                            {
                                //Core.Log.LogInfo("updatecraftpatch" + itemPrefab.LookupName());
                                playerJobs.Add(itemPrefab, 1);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Core.Log.LogInfo($"Exited UpdateCraftingSystem hook early: {e}");
        }
        finally
        {
            entities.Dispose();
        }
    }
    /*
    [HarmonyPatch(typeof(UpdateCharacterCraftingSystem), nameof(UpdateCharacterCraftingSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(UpdateCharacterCraftingSystem __instance)
    {
        NativeArray<Entity> entities = __instance.__query_970757718_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!Core.hasInitialized) continue;
                if (!Plugin.ProfessionSystem.Value) continue;

                if (entity.Has<QueuedWorkstationCraftAction>())
                {
                    var buffer = entity.ReadBuffer<QueuedWorkstationCraftAction>();
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        var item = buffer[i];
                        Entity userEntity = item.InitiateUser;
                        User user = userEntity.Read<User>();
                        ulong steamId = user.PlatformId;
                        PrefabGUID recipePrefab = item.RecipeGuid;
                        Entity recipeEntity = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[recipePrefab];
                        float totalTime = recipeEntity.Read<RecipeData>().CraftDuration; // need to account for craft rate reduction in settings here
                        float progress = item.ProgressTime;
                        Core.Log.LogInfo($"Progress: {progress}, Total Time: {totalTime} ({progress / totalTime} | {RecipeDurationMultiplier}%)");
                        if (progress / (float)totalTime >= CraftThreshold)
                        {
                            if (CraftCooldowns.TryGetValue(userEntity, out var cooldowns))
                            {
                                if (cooldowns.TryGetValue(recipePrefab, out var lastCrafted))
                                {
                                    if (DateTime.Now < lastCrafted.AddSeconds(5))
                                    {
                                        continue;
                                    }
                                }
                                else
                                {
                                    cooldowns.Add(recipePrefab, DateTime.Now);
                                }
                            }
                            else
                            {
                                CraftCooldowns.Add(userEntity, new Dictionary<PrefabGUID, DateTime> { { recipePrefab, DateTime.Now } });
                            }

                            var outputBuffer = recipeEntity.ReadBuffer<RecipeOutputBuffer>();
                            PrefabGUID itemPrefab = outputBuffer[0].Guid;
                            Core.Log.LogInfo($"charactercrafting quest {itemPrefab.LookupName()}");
                            if (Core.DataStructures.PlayerQuests.TryGetValue(steamId, out var questData))
                            {
                                QuestUtilities.UpdateQuestProgress(questData, itemPrefab, 1, user);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Core.Log.LogInfo($"Exited UpdateCraftingSystem hook early: {e}");
        }
        finally
        {
            entities.Dispose();
        }
    }
    
    [HarmonyPatch(typeof(YieldResourcesSystem_Dead), nameof(YieldResourcesSystem_Dead.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(YieldResourcesSystem_Dead __instance)
    {
        NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!Core.hasInitialized) continue;
                if (!Plugin.ProfessionSystem.Value) continue;

                //entity.LogComponentTypes();
                var buffer = entity.ReadBuffer<YieldResourcesOnDamageTaken>();
                for (int i = 0; i < buffer.Length; i++)
                {
                    var item = buffer[i];
                    Core.Log.LogInfo($"Prefab: {item.ItemType.LookupName()} | Amount: {item.Amount} | AmountTaken: {item.AmountTaken}");
                }
            }
        }
        catch (Exception e)
        {
            Core.Log.LogInfo($"Exited UpdateCraftingSystem hook early: {e}");
        }
        finally
        {
            entities.Dispose();
        }
    }
    */
}