using Bloodcraft.Services;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Systems.Legacies;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Systems.Professions;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using static Bloodcraft.Systems.Familiars.FamiliarSummonSystem;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class DeathEventListenerSystemPatch
{
    static readonly bool _leveling = ConfigService.LevelingSystem;
    static readonly bool _expertise = ConfigService.ExpertiseSystem;
    static readonly bool _familiars = ConfigService.FamiliarSystem;
    static readonly bool _legacies = ConfigService.BloodSystem;
    static readonly bool _professions = ConfigService.ProfessionSystem;
    static readonly bool _allowMinions = ConfigService.FamiliarSystem && ConfigService.AllowMinions;
    public class DeathEventArgs : EventArgs
    {
        public Entity Source { get; set; }
        public Entity Target { get; set; }
        public HashSet<Entity> DeathParticipants { get; set; }
    }
    public static event EventHandler<DeathEventArgs> OnDeathEventHandler;
    static void RaiseDeathEvent(DeathEventArgs deathEvent)
    {
        OnDeathEventHandler?.Invoke(null, deathEvent);
    }

    [HarmonyPatch(typeof(DeathEventListenerSystem), nameof(DeathEventListenerSystem.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdatePostfix(DeathEventListenerSystem __instance)
    {
        if (!Core._initialized) return;

        NativeArray<DeathEvent> deathEvents = __instance._DeathEventQuery.ToComponentDataArray<DeathEvent>(Allocator.Temp);

        ComponentLookup<Movement> movementLookup = __instance.GetComponentLookup<Movement>(true);
        ComponentLookup<BlockFeedBuff> blockFeedBuffLookup = __instance.GetComponentLookup<BlockFeedBuff>(true);
        ComponentLookup<Trader> traderLookup = __instance.GetComponentLookup<Trader>(true);
        ComponentLookup<UnitLevel> unitLevelLookup = __instance.GetComponentLookup<UnitLevel>(true);
        ComponentLookup<Minion> minionLookup = __instance.GetComponentLookup<Minion>(true);
        ComponentLookup<VBloodConsumeSource> vBloodConsumeSourceLookup = __instance.GetComponentLookup<VBloodConsumeSource>(true);

        try
        {
            foreach (DeathEvent deathEvent in deathEvents)
            {
                if (!ValidateTarget(deathEvent, ref blockFeedBuffLookup, ref traderLookup, ref unitLevelLookup, ref vBloodConsumeSourceLookup)) continue;
                else if (movementLookup.HasComponent(deathEvent.Died))
                {
                    Entity deathSource = ValidateSource(deathEvent.Killer);

                    bool isFeedKill = deathEvent.StatChangeReason.Equals(StatChangeReason.HandleGameplayEventsBase_11);
                    bool isMinion = minionLookup.HasComponent(deathEvent.Died);

                    if (deathSource.Exists())
                    {
                        DeathEventArgs deathArgs = new()
                        {
                            Source = deathSource,
                            Target = deathEvent.Died,
                            DeathParticipants = Misc.GetDeathParticipants(deathSource)
                        };

                        if (isMinion)
                        {
                            if (_allowMinions)
                            {
                                FamiliarUnlockSystem.OnUpdate(null, deathArgs);
                            }

                            continue;
                        }

                        RaiseDeathEvent(deathArgs);

                        if (_legacies && isFeedKill) BloodSystem.ProcessLegacy(deathArgs.Source, deathArgs.Target);
                    }
                }
                else if (_professions && deathEvent.Killer.IsPlayer())
                {
                    ProfessionSystem.UpdateProfessions(deathEvent.Killer, deathEvent.Died);
                }
            }
        }
        finally
        {
            deathEvents.Dispose();
        }
    }
    static Entity ValidateSource(Entity source)
    {
        Entity deathSource = Entity.Null;
        if (source.IsPlayer()) return source; // players

        if (!source.TryGetComponent(out EntityOwner entityOwner)) return deathSource;
        else if (entityOwner.Owner.TryGetPlayer(out Entity player)) deathSource = player; // player familiars and player summons
        else if (entityOwner.Owner.TryGetFollowedPlayer(out Entity followedPlayer)) deathSource = followedPlayer; // familiar summons

        return deathSource;
    }
    static bool ValidateTarget(DeathEvent deathEvent, ref ComponentLookup<BlockFeedBuff> blockFeedBuffLookup, 
        ref ComponentLookup<Trader> traderLookup, ref ComponentLookup<UnitLevel> unitLevelLookup, 
        ref ComponentLookup<VBloodConsumeSource> vBloodConsumeSourceLookup)
    {
        // if (deathEvent.Killer.IsPlayer()) Core.Log.LogInfo($"DeathEvent died - {deathEvent.Died.GetPrefabGuid()}");

        if (deathEvent.Killer == deathEvent.Died) return false;
        else if (_familiars && deathEvent.Died.TryGetFollowedPlayer(out Entity player))
        {
            ulong steamId = player.GetSteamId();

            if (steamId.TryGetFamiliarActives(out var actives) && actives.FamKey.Equals(deathEvent.Died.GetPrefabGuidHash()))
            { 
                // Familiars.UnequipFamiliar(deathEvent.Died);
                Familiars.ClearFamiliarActives(steamId);
            }

            return false;
        }
        else if (PlayerBattleFamiliars.Any() && PlayerBattleFamiliars.FirstOrDefault(kvp => kvp.Value.Contains(deathEvent.Died)) is var match && match.Key != default)
        {
            ulong ownerId = match.Key;

            PlayerBattleFamiliars[ownerId].Remove(deathEvent.Died);
            if (LinkMinionToOwnerOnSpawnSystemPatch.FamiliarMinions.ContainsKey(deathEvent.Died)) Familiars.HandleFamiliarMinions(deathEvent.Died);

            if (!PlayerBattleFamiliars[ownerId].Any() && BattleService.Matchmaker.MatchPairs.TryGetMatch(ownerId, out var matchPair))
            {
                ulong pairedId = matchPair.Item1 == ownerId ? matchPair.Item2 : matchPair.Item1;

                if (PlayerBattleFamiliars[pairedId].Any())
                {
                    foreach (Entity familiar in PlayerBattleFamiliars[pairedId])
                    {
                        if (LinkMinionToOwnerOnSpawnSystemPatch.FamiliarMinions.ContainsKey(familiar)) Familiars.HandleFamiliarMinions(familiar);
                        if (familiar.Exists()) familiar.Destroy();
                    }

                    PlayerBattleFamiliars[pairedId].Clear();
                    BattleService.Matchmaker.HandleMatchCompletion(matchPair, pairedId);
                }
            }

            return false;
        }
        else if (vBloodConsumeSourceLookup.HasComponent(deathEvent.Died) 
            || blockFeedBuffLookup.HasComponent(deathEvent.Died) 
            || traderLookup.HasComponent(deathEvent.Died) 
            || !unitLevelLookup.HasComponent(deathEvent.Died)) return false;

        return true;
    }
}