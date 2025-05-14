using Bloodcraft.Services;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Systems.Legacies;
using Bloodcraft.Systems.Professions;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using Unity.Collections;
using Unity.Entities;
using static Bloodcraft.Services.DataService.FamiliarPersistence;
using static Bloodcraft.Systems.Familiars.FamiliarBindingSystem;
using static Bloodcraft.Utilities.Familiars;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class DeathEventListenerSystemPatch
{
    static readonly bool _leveling = ConfigService.LevelingSystem;
    static readonly bool _expertise = ConfigService.ExpertiseSystem;
    static readonly bool _familiars = ConfigService.FamiliarSystem;
    static readonly bool _legacies = ConfigService.LegacySystem;
    static readonly bool _professions = ConfigService.ProfessionSystem;
    static readonly bool _allowMinions = ConfigService.FamiliarSystem && ConfigService.AllowMinions;
    public class DeathEventArgs : EventArgs
    {
        public Entity Source { get; set; }
        public Entity Target { get; set; }
        public HashSet<Entity> DeathParticipants { get; set; }
        public float ScrollingTextDelay { get; set; }
    }
    public static event EventHandler<DeathEventArgs> OnDeathEventHandler;
    static void RaiseDeathEvent(DeathEventArgs deathEvent)
    {
        OnDeathEventHandler?.Invoke(null, deathEvent);
    }

    [HarmonyPatch(typeof(DeathEventListenerSystem), nameof(DeathEventListenerSystem.OnUpdate))]
    [HarmonyPostfix]
    static unsafe void OnUpdatePostfix(DeathEventListenerSystem __instance)
    {
        if (!Core._initialized) return;

        using NativeAccessor<DeathEvent> deathEvents = __instance._DeathEventQuery.ToComponentDataArrayAccessor<DeathEvent>();

        ComponentLookup<Movement> movementLookup = __instance.GetComponentLookup<Movement>(true);
        ComponentLookup<BlockFeedBuff> blockFeedBuffLookup = __instance.GetComponentLookup<BlockFeedBuff>(true);
        ComponentLookup<Trader> traderLookup = __instance.GetComponentLookup<Trader>(true);
        ComponentLookup<UnitLevel> unitLevelLookup = __instance.GetComponentLookup<UnitLevel>(true);
        ComponentLookup<Minion> minionLookup = __instance.GetComponentLookup<Minion>(true);
        ComponentLookup<VBloodConsumeSource> vBloodConsumeSourceLookup = __instance.GetComponentLookup<VBloodConsumeSource>(true);
        
        try
        {
            for (int i = 0; i < deathEvents.Length; i++)
            {
                DeathEvent deathEvent = deathEvents[i];

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
                            DeathParticipants = Progression.GetDeathParticipants(deathSource),
                            ScrollingTextDelay = 0f
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

                        if (_legacies && isFeedKill)
                        {
                            // deathArgs.RefreshStats = false;
                            BloodSystem.ProcessLegacy(deathArgs);
                        }
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
            // deathEvents.Dispose();
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
        if (deathEvent.Killer == deathEvent.Died) return false;
        else if (_familiars && deathEvent.Died.TryGetFollowedPlayer(out Entity playerCharacter))
        {
            ulong steamId = playerCharacter.GetSteamId();
            bool hasActive = steamId.HasActiveFamiliar();

            if (hasActive)
            { 
                Entity familiar = GetActiveFamiliar(playerCharacter);

                if (familiar.Equals(deathEvent.Died))
                {
                    // FamiliarEquipmentManager.UnequipFamiliar(GetFamiliarServant(playerCharacter));
                    Entity familiarServant = GetFamiliarServant(playerCharacter);

                    familiarServant.TryRemove<Disabled>();
                    familiarServant.TryDestroy();

                    ActiveFamiliarManager.ResetActiveFamiliarData(steamId);
                }
            }

            return false;
        }
        else if (PlayerBattleFamiliars.Any() && PlayerBattleFamiliars.FirstOrDefault(kvp => kvp.Value.Contains(deathEvent.Died)) is var match && match.Key != default)
        {
            ulong ownerId = match.Key;

            PlayerBattleFamiliars[ownerId].Remove(deathEvent.Died);
            if (LinkMinionToOwnerOnSpawnSystemPatch.FamiliarMinions.ContainsKey(deathEvent.Died)) HandleFamiliarMinions(deathEvent.Died);

            if (!PlayerBattleFamiliars[ownerId].Any() && BattleService.Matchmaker.MatchPairs.TryGetMatch(ownerId, out var matchPair))
            {
                ulong pairedId = matchPair.Item1 == ownerId ? matchPair.Item2 : matchPair.Item1;

                if (PlayerBattleFamiliars[pairedId].Any())
                {
                    foreach (Entity familiar in PlayerBattleFamiliars[pairedId])
                    {
                        if (LinkMinionToOwnerOnSpawnSystemPatch.FamiliarMinions.ContainsKey(familiar)) HandleFamiliarMinions(familiar);
                        if (familiar.Exists()) familiar.TryDestroy();
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