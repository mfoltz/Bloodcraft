using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using User = ProjectM.Network.User;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class CraftingPatches
{
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

    [HarmonyPatch(typeof(ForgeSystem_Events), nameof(ForgeSystem_Events.OnUpdate))]
    static class ForgeSystem_EventsPatch
    {
        public static void Prefix(ForgeSystem_Events __instance)
        {
            var repairEntities = __instance._RepairItemEventQuery.ToEntityArray(Allocator.Temp);
            //var cancelRepairEntities = __instance._CancelRepairEventQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (Entity entity in repairEntities)
                {
                    var fromCharacter = entity.Read<FromCharacter>();
                    ulong steamId = fromCharacter.User.Read<User>().PlatformId;
                    Entity station = fromCharacter.Character.Read<Interactor>().Target; // station entity

                    if (!station.Has<Forge_Shared>()) continue;

                    Forge_Shared forge_Shared = station.Read<Forge_Shared>();
                    Entity itemEntity = forge_Shared.ItemEntity._Entity;
                    PrefabGUID itemPrefab = new(0);
                    if (itemEntity.Has<ShatteredItem>())
                    {
                        itemPrefab = itemEntity.Read<ShatteredItem>().OutputItem;
                    }
                    else if (itemEntity.Has<UpgradeableLegendaryItem>())
                    {
                        int tier = itemEntity.Read<UpgradeableLegendaryItem>().NextTier;
                        var buffer = itemEntity.ReadBuffer<UpgradeableLegendaryItemTiers>();
                        itemPrefab = buffer[tier].TierPrefab;
                    }

                    if (itemPrefab.GuidHash == 0) continue;

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
            finally
            {
                repairEntities.Dispose();
            }
            repairEntities = __instance._CancelRepairEventQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (Entity entity in repairEntities)
                {
                    var fromCharacter = entity.Read<FromCharacter>();
                    ulong steamId = fromCharacter.User.Read<User>().PlatformId;
                    Entity station = fromCharacter.Character.Read<Interactor>().Target; // station entity

                    if (!station.Has<Forge_Shared>()) continue;

                    Forge_Shared forge_Shared = station.Read<Forge_Shared>();
                    Entity itemEntity = forge_Shared.ItemEntity._Entity;
                    PrefabGUID itemPrefab = new(0);
                    if (itemEntity.Has<ShatteredItem>())
                    {
                        itemPrefab = itemEntity.Read<ShatteredItem>().OutputItem;
                    }
                    else if (itemEntity.Has<UpgradeableLegendaryItem>())
                    {
                        int tier = itemEntity.Read<UpgradeableLegendaryItem>().NextTier;
                        var buffer = itemEntity.ReadBuffer<UpgradeableLegendaryItemTiers>();
                        itemPrefab = buffer[tier].TierPrefab;
                    }

                    if (itemPrefab.GuidHash == 0) continue;

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
            finally
            {
                repairEntities.Dispose();
            }
        }
    }
}