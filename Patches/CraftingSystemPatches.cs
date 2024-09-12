using Bloodcraft.Services;
using Bloodcraft.Systems.Professions;
using HarmonyLib;
using ProjectM;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using User = ProjectM.Network.User;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class CraftingSystemPatches // ForgeSystem_Update, UpdateCraftingSystem
{
    static SystemService SystemService => Core.SystemService;
    static PrefabCollectionSystem PrefabCollectionSystem => SystemService.PrefabCollectionSystem;

    const float CraftThreshold = 0.995f;
    static readonly float CraftRateModifier = SystemService.ServerGameSettingsSystem.Settings.CraftRateModifier;

    static readonly Dictionary<Entity, Dictionary<PrefabGUID, DateTime>> CraftCooldowns = [];

    [HarmonyPatch(typeof(ForgeSystem_Update), nameof(ForgeSystem_Update.OnUpdate))]
    [HarmonyPrefix]
    static void Prefix(ForgeSystem_Update __instance)
    {
        var repairEntities = __instance.__query_1536473549_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in repairEntities)
            {
                if (!Core.hasInitialized) return;

                if (!ConfigService.ProfessionSystem) continue;

                Forge_Shared forge_Shared = entity.Read<Forge_Shared>();
                if (forge_Shared.State == ForgeState.Empty) continue;

                UserOwner userOwner = entity.Read<UserOwner>();
                Entity userEntity = userOwner.Owner._Entity;
                User user = userEntity.Read<User>();
                ulong steamId = user.PlatformId;

                Entity itemEntity = forge_Shared.ItemEntity._Entity;
                PrefabGUID itemPrefab = itemEntity.Read<PrefabGUID>();

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

                if (forge_Shared.State == ForgeState.Finished)
                {
                    //Core.Log.LogInfo($"Forge finished: {itemPrefab.LookupName()} | {itemEntity.Read<PrefabGUID>().LookupName()}");
                    float ProfessionValue = 50f;
                    ProfessionValue *= ProfessionMappings.GetTierMultiplier(itemPrefab);
                    IProfessionHandler handler = ProfessionHandlerFactory.GetProfessionHandler(itemPrefab, "");
                    if (handler != null)
                    {
                        if (itemEntity.Has<Durability>())
                        {
                            Entity originalItem = PrefabCollectionSystem._PrefabGuidToEntityMap[itemPrefab];

                            Durability durability = itemEntity.Read<Durability>();
                            Durability originalDurability = originalItem.Read<Durability>();

                            if (durability.MaxDurability != originalDurability.MaxDurability) continue; // already handled

                            int level = handler.GetProfessionData(steamId).Key;

                            durability.MaxDurability *= (1 + (float)level / (float)ConfigService.MaxProfessionLevel);
                            durability.Value = durability.MaxDurability;
                            itemEntity.Write(durability);

                            ProfessionSystem.SetProfession(user, steamId, ProfessionValue, handler);
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

    [HarmonyPatch(typeof(UpdateCraftingSystem), nameof(UpdateCraftingSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(UpdateCraftingSystem __instance)
    {
        NativeArray<Entity> entities = __instance.__query_1831452865_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!Core.hasInitialized) return;
                if (!ConfigService.ProfessionSystem) return;

                //Core.Log.LogInfo("Check 1");
                if (!entity.Exists()) continue;

                if (entity.Has<CastleWorkstation>() && entity.Has<QueuedWorkstationCraftAction>())
                {
                    //Core.Log.LogInfo("Check 2");
                    var buffer = entity.ReadBuffer<QueuedWorkstationCraftAction>();
                    double recipeReduction = entity.Read<CastleWorkstation>().WorkstationLevel.HasFlag(WorkstationLevel.MatchingFloor) ? 0.75 : 1;

                    //Core.Log.LogInfo("Check 3");
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        var item = buffer[i];

                        //Core.Log.LogInfo("Check 4");
                        Entity userEntity = item.InitiateUser;
                        User user = userEntity.Read<User>();
                        ulong steamId = user.PlatformId;

                        //Core.Log.LogInfo("Check 5");
                        PrefabGUID recipePrefab = item.RecipeGuid;
                        Entity recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap.ContainsKey(recipePrefab) ? PrefabCollectionSystem._PrefabGuidToEntityMap[recipePrefab] : Entity.Null;
                        double totalTime = recipeEntity.Read<RecipeData>().CraftDuration * recipeReduction;

                        //Core.Log.LogInfo("Check 6");
                        if (CraftRateModifier != 1f)
                        {
                            totalTime /= CraftRateModifier;
                        }

                        //Core.Log.LogInfo("Check 7");
                        float progress = item.ProgressTime;
                        if (progress / (float)totalTime >= CraftThreshold)
                        {
                            DateTime now = DateTime.UtcNow;

                            //Core.Log.LogInfo("Check 8");
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
                                    cooldowns.TryAdd(recipePrefab, now);
                                }
                            }
                            else
                            {
                                //Core.Log.LogInfo($"Adding stamp for {recipePrefab.LookupName()} at: {now}");
                                CraftCooldowns.TryAdd(userEntity, new Dictionary<PrefabGUID, DateTime> { { recipePrefab, now } });
                            }

                            var outputBuffer = recipeEntity.ReadBuffer<RecipeOutputBuffer>();
                            PrefabGUID itemPrefab = outputBuffer[0].Guid;

                            if (steamId.TryGetPlayerCraftingJobs(out var playerJobs))
                            {
                                if (playerJobs.TryGetValue(itemPrefab, out var recipeJobs))
                                {
                                    //Core.Log.LogInfo("updatecraftpatch" + itemPrefab.LookupName());
                                    recipeJobs++;
                                }
                                else
                                {
                                    //Core.Log.LogInfo("updatecraftpatch" + itemPrefab.LookupName());
                                    playerJobs.TryAdd(itemPrefab, 1);
                                }
                            }
                            else
                            {
                                steamId.SetPlayerCraftingJobs([]);
                                if (steamId.TryGetPlayerCraftingJobs(out playerJobs))
                                {
                                    if (playerJobs.TryGetValue(itemPrefab, out var recipeJobs))
                                    {
                                        //Core.Log.LogInfo("updatecraftpatch" + itemPrefab.LookupName());
                                        recipeJobs++;
                                    }
                                    else
                                    {
                                        //Core.Log.LogInfo("updatecraftpatch" + itemPrefab.LookupName());
                                        playerJobs.TryAdd(itemPrefab, 1);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Core.Log.LogError("UpdateCraftingSystem error..." + e);
        }
        finally
        {
            entities.Dispose();
        }
    }
}