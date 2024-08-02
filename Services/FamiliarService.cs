using Bloodcraft.Patches;
using Bloodcraft.Systems.Familiars;
using ProjectM;
using ProjectM.Behaviours;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using System.Collections;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Bloodcraft.Services;
internal class FamiliarService 
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static FactionLookupSingleton FactionLookupSingleton => Core.FactionLookupSingleton;

    static bool active = false;

    public static List<Entity> ActiveFamiliars = [];
    public FamiliarService() // coroutine to occasionally move familiars to owners if disabled? hmmm
    {
        List<int> unitBans = Core.ParseConfigString(Plugin.BannedUnits.Value);
        List<string> typeBans = Plugin.BannedTypes.Value.Split(',').Select(s => s.Trim()).ToList();
        if (unitBans.Count > 0) FamiliarUnlockUtilities.ExemptPrefabs = unitBans;
        if (typeBans.Count > 0) FamiliarUnlockUtilities.ExemptTypes = typeBans;
        //Core.StartCoroutine(CleanUpMinions());
    }
    public static void StartFamiliarHandler(Entity familiar)
    {
        if (!active) // if not active start monitor loop after clearing caches
        {
            ActiveFamiliars.Add(familiar);
            Core.Log.LogInfo("Starting Familiar Combat Loop...");
            Core.StartCoroutine(FamiliarCombatLoop());
        }
        else if (active) // if active update onlinePlayers and add new territory participants
        {
            ActiveFamiliars.Add(familiar);
        }
    }
    unsafe static void GetFactionIndices()
    {
        NativeArray<Entity> entities = FactionLookupSingleton.FactionPrefabEntityLookup;
        try
        {
            foreach (Entity entity in entities)
            {
                /*
                if (entity.Read<PrefabGUID>().Equals(Ignored))
                {
                    // Get BlobAssetReference from Faction
                    Faction playerMutantFaction = entity.Read<Faction>();
                    BlobAssetReference<FactionBlobAsset> blobAssetReferencePlayerMutantFaction = playerMutantFaction.Data;
                    FactionBlobAsset* playerMutantFactionBlobAsset = (FactionBlobAsset*)blobAssetReferencePlayerMutantFaction.GetUnsafePtr();

                    // Allocate memory for the new FactionBlobAsset
                    long size = UnsafeUtility.SizeOf<FactionBlobAsset>();
                    byte* allocatedMemory = (byte*)UnsafeUtility.Malloc(size, 16, Allocator.Persistent);

                    // Copy existing data
                    UnsafeUtility.MemCpy(allocatedMemory, playerMutantFactionBlobAsset, size);

                    // Cast the allocated memory to FactionBlobAsset
                    FactionBlobAsset* newFactionBlobAsset = (FactionBlobAsset*)allocatedMemory;

                    // Update faction values as desired
                    newFactionBlobAsset->Membership = Membership;
                    newFactionBlobAsset->Hostile = Hostile;
                    newFactionBlobAsset->Friendly = Friendly;
                    newFactionBlobAsset->Neutral = Neutral;

                    // Create the new BlobAssetReference from the allocated memory
                    BlobAssetReference<FactionBlobAsset> newBlobAssetReference = BlobAssetReference<FactionBlobAsset>.Create(newFactionBlobAsset, (int)size);

                    // Assign the new BlobAssetReference back to the Faction component
                    playerMutantFaction.Data = newBlobAssetReference;
                    entity.Write(playerMutantFaction);

                    // Free the temporary allocated memory and break
                    UnsafeUtility.Free(allocatedMemory, Allocator.Persistent);
                }
                */

                Faction faction = entity.Read<Faction>();
                BlobAssetReference<FactionBlobAsset> blobAssetReference = faction.Data;
                FactionBlobAsset* factionBlobAsset = (FactionBlobAsset*)blobAssetReference.GetUnsafePtr();

                PrefabGUID factionPrefab = entity.Read<PrefabGUID>();
                int factionIndex = factionBlobAsset->Index;
                FamiliarSummonUtilities.FactionIndices.TryAdd(factionPrefab, factionIndex);

                /*
                Array factions = Enum.GetValues(typeof(FactionEnum));
                Core.Log.LogInfo($"Faction: {entity.Read<PrefabGUID>().LookupName()} | Index: {factionBlobAsset->Index}");
                Core.Log.LogInfo($"Members: {string.Join(", ", CollectEnumValues(factionBlobAsset->Membership, factions))}");
                Core.Log.LogInfo($"Friendly: {string.Join(", ", CollectEnumValues(factionBlobAsset->Friendly, factions))}");
                Core.Log.LogInfo($"Hostile: {string.Join(", ", CollectEnumValues(factionBlobAsset->Hostile, factions))}");
                Core.Log.LogInfo($"Neutral: {string.Join(", ", CollectEnumValues(factionBlobAsset->Neutral, factions))}");
                */
            }
            /*
            foreach (var sourceFaction in FamiliarSummonUtilities.FactionIndices)
            {
                foreach (var targetFaction in FamiliarSummonUtilities.FactionIndices)
                {
                    int sourceIndex = sourceFaction.Value;
                    int targetIndex = targetFaction.Value;
                    float aggroMultiplier = FactionLookupSingleton.GetAggroMultiplier(sourceIndex, targetIndex);
                    Core.Log.LogInfo($"Aggro Multiplier from {sourceFaction.Key.LookupName()} to {targetFaction.Key.LookupName()}: {aggroMultiplier}");
                }
            }
            */
        }
        catch (Exception e)
        {
            Core.Log.LogError($"Error: {e}");
        }
    }
    static List<string> CollectEnumValues(FactionEnum factionEnum, Array factions)
    {
        List<string> values = [];
        foreach (FactionEnum faction in factions)
        {
            if (factionEnum.HasFlag(faction) && faction != FactionEnum.None)
            {
                values.Add(faction.ToString());
            }
        }
        return values;
    }
    static IEnumerator FamiliarCombatLoop()
    {
        active = true;

        while (true)
        {
            if (ActiveFamiliars.Count == 0)
            {
                active = false;
                yield break;
            }
            else
            {
                List<Entity> familiars = new(ActiveFamiliars);
                foreach (Entity familiar in familiars)
                {
                    if (!EntityManager.Exists(familiar) || familiar.Read<BehaviourTreeState>().Equals(GenericEnemyState.Follow))
                    {
                        ActiveFamiliars.Remove(familiar);
                        continue;
                    }
                    else
                    {
                        var aggroBuffer = familiar.ReadBuffer<AggroBuffer>();
                        var alertBuffer = familiar.ReadBuffer<AlertBuffer>();
                        AggroConsumer aggroConsumer = familiar.Read<AggroConsumer>();
                        //var candidateBuffer = familiar.ReadBuffer<AggroCandidateBufferElement>();
                        //var damageHistoryBuffer = familiar.ReadBuffer<AggroDamageHistoryBufferElement>();
                        //var externalBuffer = familiar.ReadBuffer<ExternalAggroBufferElement>();
                        for (int i = 0; i < alertBuffer.Length; i++)
                        {
                            var item = alertBuffer[i];
                            if (item.Entity.Has<PlayerCharacter>())
                            {
                                Core.Log.LogInfo($"[Player] - Alert Value: {item.Value}");
                                //item.Value = 0;
                                //alertBuffer[i] = item;
                                //Core.Log.LogInfo($"[Player] - Alert Value: {item.Value}");
                            }
                        }
                        for (int i = 0; i < aggroBuffer.Length; i++)
                        {
                            var item = aggroBuffer[i];
                            if (item.IsPlayer)
                            {
                                Core.Log.LogInfo($"[Player] - Damage Value: {item.DamageValue} | External Value: {item.ExternalValue} | Proximity Value: {item.ProximityValue} | Weight: {item.Weight}");
                                //item.DamageValue = 0;
                                //item.ExternalValue = 0;
                                //item.ProximityValue = 0;
                                //item.Weight = 0;
                                //aggroBuffer[i] = item;
                                //Core.Log.LogInfo($"[Player] - Damage Value: {item.DamageValue} | External Value: {item.ExternalValue} | Proximity Value: {item.ProximityValue} | Weight: {item.Weight}");
                            }
                        }
                        if (!aggroConsumer.AggroTarget._Entity.Equals(Entity.Null))
                        {
                            Core.Log.LogInfo($"[Familiar] - Aggro Target: {aggroConsumer.AggroTarget._Entity.Read<PrefabGUID>().LookupName()}");
                            AiMove_Server aiMove_Server = familiar.Read<AiMove_Server>();
                            aiMove_Server.TargetPosition = aggroConsumer.AggroTarget._Entity.Read<Translation>().Value.xz;
                            aiMove_Server.TargettingMode = AiTargettingMode.Position;
                            familiar.Write(aiMove_Server);
                            Core.Log.LogInfo($"[Familiar] - Target Position Set: {aiMove_Server.TargetPosition}");
                        }
                        if (!aggroConsumer.AlertTarget._Entity.Equals(Entity.Null))
                        {
                            Core.Log.LogInfo($"[Familiar] - Alert Target: {aggroConsumer.AlertTarget._Entity.Read<PrefabGUID>().LookupName()}");
                        }
                    }
                    yield return null;
                }
            }
        }
    }
    /*
    void HandleFamiliarsOnSpawn()
    {
        NativeArray<Entity> followers = familiarQuery.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity follower in followers)
            {
                if (follower.Read<Follower>().Followed._Value.Has<PlayerCharacter>())
                {
                    //if (follower.Has<MinionMaster>()) HandleFamiliarMinions(follower);
                    DestroyUtility.CreateDestroyEvent(EntityManager, follower, DestroyReason.Default, DestroyDebugReason.None);
                    ulong steamId = follower.Read<Follower>().Followed._Value.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
                    if (Core.DataStructures.FamiliarActives.TryGetValue(steamId, out var actives))
                    {
                        Core.DataStructures.FamiliarActives[steamId] = new(Entity.Null, 0);
                        Core.DataStructures.SavePlayerFamiliarActives();
                    }
                }
            }
        }
        finally
        {
            followers.Dispose();
        }      
    }
    
    void FindAndHandleFamiliarMinions()
    {
        NativeArray<Entity> minions = minionQuery.ToEntityArray(Allocator.Temp);
        HashSet<Entity> players = [..PlayerService.PlayerCache.Values];
        var unholyPrefix = "char_unholy";
        try
        {
            foreach (Entity minion in minions)
            {
                string minionName = minion.Read<PrefabGUID>().LookupName().ToLower();
                if (minionName.Contains(unholyPrefix)) continue;
                foreach (Entity player in players)
                {
                    if (ServerGameManager.IsAllies(minion, player))
                    {
                        //Core.Log.LogInfo($"Destroying minion {minionName}...");
                        DestroyUtility.CreateDestroyEvent(EntityManager, minion, DestroyReason.Default, DestroyDebugReason.None);
                        break;
                    }
                }
            }
        }
        finally
        {
            minions.Dispose();
        }
    }
    */
    public void HandleFamiliarMinions(Entity familiar)
    {
        if (FamiliarPatches.FamiliarMinions.ContainsKey(familiar))
        {
            foreach (Entity minion in FamiliarPatches.FamiliarMinions[familiar])
            {
                //Core.Log.LogInfo($"Destroying minion...");
                DestroyUtility.CreateDestroyEvent(EntityManager, minion, DestroyReason.Default, DestroyDebugReason.None);
            }
            FamiliarPatches.FamiliarMinions.Remove(familiar);
        }
    }
}
