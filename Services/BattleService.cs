using Bloodcraft.Patches;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Utilities;
using Il2CppInterop.Runtime;
using ProjectM;
using Stunlock.Core;
using System.Collections;
using System.Collections.Concurrent;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static Bloodcraft.Patches.SpawnTransformSystemOnSpawnPatch;
using static Bloodcraft.Services.BattleService.Matchmaker;
using static Bloodcraft.Services.DataService.PlayerDictionaries;
using static Bloodcraft.Services.PlayerService;

namespace Bloodcraft.Services;
internal class BattleService
{
    static EntityManager EntityManager => Core.EntityManager;
    static SystemService SystemService => Core.SystemService;
    static EndSimulationEntityCommandBufferSystem EndSimulationEntityCommandBufferSystem => SystemService.EndSimulationEntityCommandBufferSystem;

    public static readonly List<float3> FamiliarBattleCoords = []; // using list for potential expansion later
    public static readonly List<int> FamiliarBattleTeams = [];

    public static readonly List<float3> PlayerOneFamiliarPositions = [];
    public static readonly List<float3> PlayerTwoFamiliarPositions = [];

    static readonly WaitForSeconds _challengeExpiration = new(CHALLENGE_EXPIRATION);
    static readonly WaitForSeconds _battleInterval = new(BATTLE_INTERVAL);
    static readonly WaitForSeconds _timeoutDelay = new(FamiliarSummonSystem.FAMILIAR_LIFETIME - MATCH_START_COUNTDOWN);

    static readonly WaitForSeconds _secondDelay = new(1f);

    static readonly ComponentType[] _unitTeamComponent =
    [
        ComponentType.ReadOnly(Il2CppType.Of<UnitTeam>()),
    ];

    static readonly AssetGuid _assetGuid = AssetGuid.FromString("2a1f5c1b-5a50-4ff0-a982-ca37efb8f69d");
    static readonly PrefabGUID _battleSCT = new(106212079); // InfoWarning SCT, good enough
    static readonly float3 _green = new(0f, 1f, 0f);

    public static float3 _battlePosition = float3.zero;
    public static float3 _sCTPosition = float3.zero;

    const float MATCH_START_COUNTDOWN = 5f;
    const float BATTLE_INTERVAL = 300f;
    const float CHALLENGE_EXPIRATION = 30f;
    const float UNIT_SPACING = 2.5f;
    const float TEAM_DISTANCE = 3f;
    const float SPECTATE_DISTANCE = 25f;
    const float SCT_HEIGHT = 15f;

    public const int TEAM_SIZE = 3;

    static DateTime _matchPendingStart;
    static bool _serviceActive = false;
    static bool _matchPending = false;
    public BattleService()
    {
        Initialize();
    }
    public static void Initialize()
    {
        if (_familiarBattleCoords.Any())
        {
            List<float> floats = _familiarBattleCoords.FirstOrDefault();
            float3 battlePosition = new(floats[0], floats[1], floats[2]);
            FamiliarBattleCoords.Add(battlePosition);

            _battlePosition = new(battlePosition.x, battlePosition.y, battlePosition.z);
            _sCTPosition = new(battlePosition.x, battlePosition.y + SCT_HEIGHT, battlePosition.z);

            GenerateBattleFormations(battlePosition);
            Core.StartCoroutine(BattleRoutine());

            _serviceActive = true;

            try
            {
                EntityQuery unitTeamQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
                {
                    All = _unitTeamComponent,
                    Options = EntityQueryOptions.IncludeDisabled
                });

                NativeArray<Entity> entities = unitTeamQuery.ToEntityArray(Allocator.TempJob);
                try
                {
                    foreach (Entity entity in entities)
                    {
                        if (entity.Has<UnitTeam>())
                        {
                            FamiliarSummonSystem._unitTeam = entity;
                        }
                    }
                }
                finally
                {
                    entities.Dispose();
                    unitTeamQuery.Dispose();
                }
            }
            catch (Exception ex)
            {
                Core.Log.LogError($"Error initializing UnitTeam in BattleService - {ex}");
            }
        }
    }
    public static class Matchmaker // announce matches and stuff to people inside a radius around the set position?
    {
        public static readonly ConcurrentQueue<(ulong, ulong)> MatchQueue = new();
        public static readonly HashSet<(ulong, ulong)> MatchPairs = [];
        public static readonly HashSet<ulong> QueuedPlayers = [];
        public static readonly ConcurrentDictionary<ulong, List<PrefabGUID>> QueuedBattleGroups = new();
        public static readonly List<(ulong, ulong)> CancelledPairs = []; // list to handle multiple cancels

        // Add a pair of players to the queue if eligible
        public static void QueueMatch((ulong, ulong) match)
        {
            ulong playerOne = match.Item1;
            ulong playerTwo = match.Item2;

            if (!_serviceActive)
            {
                NotifyBothPlayers(playerOne, playerTwo, "Battle service inactive, arena position hasn't been set.");
                return;
            }
            else if (!VerifyEligible(playerOne, playerTwo)) return;

            // Block both players
            QueuedPlayers.Add(playerOne);
            QueuedPlayers.Add(playerTwo);

            // Add to match queue
            (ulong, ulong) matchPair = new(playerOne, playerTwo);
            MatchQueue.Enqueue(matchPair);
            MatchPairs.Add(matchPair);

            // Get position in queue and time remaining
            var (position, timeRemaining) = GetQueuePositionAndTime(playerOne);

            // Inform players
            string message = $"Queued successfully! Position in queue: <color=white>{position}</color> (<color=yellow>{Misc.FormatTimespan(timeRemaining)}</color>)";
            NotifyBothPlayers(playerOne, playerTwo, message);
        }

        // Match end via elimination
        public static void HandleMatchCompletion((ulong, ulong) matchPair, ulong winner)
        {
            QueuedPlayers.Remove(matchPair.Item1);
            QueuedPlayers.Remove(matchPair.Item2);
            MatchPairs.Remove(matchPair);

            foreach (Entity familiar in PlayerBattleFamiliars[matchPair.Item1])
            {
                if (familiar.Exists()) familiar.Destroy();
            }

            foreach (Entity familiar in PlayerBattleFamiliars[matchPair.Item2])
            {
                if (familiar.Exists()) familiar.Destroy();
            }

            PlayerBattleFamiliars[matchPair.Item1].Clear();
            PlayerBattleFamiliars[matchPair.Item2].Clear();

            if (matchPair.TryGetMatchPairInfo(out (PlayerInfo, PlayerInfo) matchPairInfo))
            {
                // other logic after match ends, inform players of outcome handle rewards or whatever
                PlayerInfo winnerInfo = winner == matchPairInfo.Item1.User.PlatformId ? matchPairInfo.Item1 : matchPairInfo.Item2;
                PlayerInfo loserInfo = winner == matchPairInfo.Item1.User.PlatformId ? matchPairInfo.Item2 : matchPairInfo.Item1;

                string loserName = loserInfo.User.CharacterName.Value;
                string loserNameWithSuffix = loserName.EndsWith("s", StringComparison.OrdinalIgnoreCase)
                ? $"{loserName}’"
                : $"{loserName}’s";
                string formattedLoserName = $"<color=#808080>{loserNameWithSuffix}</color>";

                NotifyBothPlayers(matchPair.Item1, matchPair.Item2, $"{formattedLoserName} familiars have been defeated! Winner: <color=#E5B800>{winnerInfo.User.CharacterName.Value}</color>");
            }
        }

        // Match end via timeout
        public static void HandleMatchTimeout((ulong, ulong) matchPair)
        {
            QueuedPlayers.Remove(matchPair.Item1);
            QueuedPlayers.Remove(matchPair.Item2);
            MatchPairs.Remove(matchPair);

            if (PlayerBattleFamiliars.TryRemove(matchPair.Item1, out List<Entity> playerOneFamiliars)
                && PlayerBattleFamiliars.TryRemove(matchPair.Item2, out List<Entity> playerTwoFamiliars))
            {
                int playerOneRemaining = PlayerBattleFamiliars[matchPair.Item1].Count;
                int playerTwoRemaining = PlayerBattleFamiliars[matchPair.Item2].Count;

                ulong winner = playerOneRemaining > playerTwoRemaining
                    ? matchPair.Item1
                    : playerTwoRemaining > playerOneRemaining
                        ? matchPair.Item2
                        : 0;

                if (matchPair.TryGetMatchPairInfo(out (PlayerInfo, PlayerInfo) matchPairInfo) && winner != 0)
                {
                    PlayerInfo winnerInfo = winner == matchPairInfo.Item1.User.PlatformId ? matchPairInfo.Item1 : matchPairInfo.Item2;
                    PlayerInfo loserInfo = winner == matchPairInfo.Item1.User.PlatformId ? matchPairInfo.Item2 : matchPairInfo.Item1;

                    string loserName = loserInfo.User.CharacterName.Value;
                    string formattedLoserName = $"<color=#808080>{loserName}</color>";

                    NotifyBothPlayers(matchPair.Item1, matchPair.Item2, $"{formattedLoserName} has the fewest familiars remaining! Winner: <color=#E5B800>{winnerInfo.User.CharacterName.Value}</color>");
                }
                else
                {
                    NotifyBothPlayers(matchPair.Item1, matchPair.Item2, "Match timeout, result is a tie! Better luck next time.");
                }

                foreach (Entity familiar in playerOneFamiliars)
                {
                    if (LinkMinionToOwnerOnSpawnSystemPatch.FamiliarMinions.ContainsKey(familiar)) Familiars.HandleFamiliarMinions(familiar);
                    if (familiar.Exists()) familiar.Destroy();
                }

                foreach (Entity familiar in playerTwoFamiliars)
                {
                    if (LinkMinionToOwnerOnSpawnSystemPatch.FamiliarMinions.ContainsKey(familiar)) Familiars.HandleFamiliarMinions(familiar);
                    if (familiar.Exists()) familiar.Destroy();
                }
            }
        }
    }
    static IEnumerator BattleRoutine()
    {
        while (true)
        {
            if (!_matchPending)
            {
                Core.Log.LogInfo("Setting time stamp at loop start...");
                _matchPending = true;
                _matchPendingStart = DateTime.UtcNow;
            }

            yield return _battleInterval;

            Core.Log.LogInfo("Checking for pending matches in queue...");

            if (MatchQueue.Any())
            {
                while (MatchQueue.TryDequeue(out var match))
                {
                    Core.Log.LogInfo("Starting match from queue...");

                    // Check if the match is in the cancelled pairs
                    if (CancelledPairs.Contains(match))
                    {
                        // Skip this match
                        CancelledPairs.Remove(match);
                        continue;
                    }
                    else
                    {
                        ulong playerOne = match.Item1;
                        ulong playerTwo = match.Item2;

                        // Validate the players
                        if (playerOne.TryGetPlayerInfo(out PlayerInfo playerOneInfo) &&
                            playerTwo.TryGetPlayerInfo(out PlayerInfo playerTwoInfo))
                        {
                            // Found a valid match
                            Core.Log.LogInfo("PlayerInfo acquired, invoking HandleBattleSummoning...");

                            SetDirectionAndFaction.Add(playerTwo);
                            HandleBattleSummoning(playerOneInfo, playerTwoInfo, playerOne, playerTwo);
                            _matchPending = false;

                            break;
                        }
                    }
                }
            }
            else
            {
                Core.Log.LogInfo("No pending matches in queue...");
                _matchPending = false;
            }

            /*
            if (MatchQueue.TryDequeue(out var match))
            {
                Core.Log.LogInfo("Starting match from queue...");

                // Check if the match is in the cancelled pairs
                if (CancelledPairs.Contains(match))
                {
                    // Skip this match
                    CancelledPairs.Remove(match);
                }
                else
                {
                    ulong playerOne = match.Item1;
                    ulong playerTwo = match.Item2;

                    // Validate the players
                    if (playerOne.TryGetPlayerInfo(out PlayerInfo playerOneInfo) &&
                        playerTwo.TryGetPlayerInfo(out PlayerInfo playerTwoInfo))
                    {
                        // Found a valid match
                        Core.Log.LogInfo("PlayerInfo acquired, invoking HandleBattleSummoning...");

                        SetRotation.Add(playerTwo);
                        HandleBattleSummoning(playerOneInfo, playerTwoInfo, playerOne, playerTwo);
                        _matchPending = false;
                    }
                }
            }
            else
            {
                Core.Log.LogInfo("No pending matches in queue...");
                _matchPending = false;
            }
            */
        }
    }
    static void HandleBattleSummoning(PlayerInfo playerOneInfo, PlayerInfo playerTwoInfo, ulong playerOne, ulong playerTwo)
    {
        if (QueuedBattleGroups.TryRemove(playerOne, out var battleGroupOne) && QueuedBattleGroups.TryRemove(playerTwo, out var battleGroupTwo))
        {
            Core.Log.LogInfo("Battle groups popped, invoking BattleSummoningRoutine...");

            PlayerFamiliarBattleGroups[playerOne] = battleGroupOne;
            PlayerFamiliarBattleGroups[playerTwo] = battleGroupTwo;

            PlayerSummoningForBattle[playerOne] = true;
            PlayerSummoningForBattle[playerTwo] = true;

            Core.StartCoroutine(BattleSummoningRoutine(playerOneInfo.CharEntity, playerOneInfo.UserEntity,
                playerTwoInfo.CharEntity, playerTwoInfo.UserEntity,
                new(battleGroupOne), new(battleGroupTwo)));
        }
        else
        {
            Core.Log.LogInfo("Couldn't pop battle groups for one or both players...");
        }
    }
    static IEnumerator BattleSummoningRoutine(Entity playerOne, Entity playerUserOne, Entity playerTwo, Entity playerUserTwo,
        List<PrefabGUID> playerOneFamiliars, List<PrefabGUID> playerTwoFamiliars)
    {
        for (int i = 0; i < TEAM_SIZE; i++)
        {
            FamiliarSummonSystem.SummonFamiliarForBattle(playerOne, playerUserOne, playerOneFamiliars[i], PlayerOneFamiliarPositions[i]);
            FamiliarSummonSystem.SummonFamiliarForBattle(playerTwo, playerUserTwo, playerTwoFamiliars[i], PlayerTwoFamiliarPositions[i]);

            yield return null;
        }
    }
    public static IEnumerator BattleStartCountdown((ulong playerOne, ulong playerTwo) matchPair)
    {
        if (!matchPair.TryGetMatchPairInfo(out (PlayerInfo, PlayerInfo) matchPairInfo))
        {
            Core.Log.LogWarning("Failed to get match pair info during battle start countdown...");

            yield break;
        }

        float countdown = MATCH_START_COUNTDOWN; // maybe send messages as well, see about the spectator stuff too
        List<PlayerInfo> onlineNearbyPlayers = OnlineCache.Values
            .Where(player => Vector3.Distance(_battlePosition, player.CharEntity.GetPosition()) < SPECTATE_DISTANCE)
            .ToList();

        ulong steamIdOne = matchPair.playerOne;
        ulong steamIdTwo = matchPair.playerTwo;

        while (countdown > 0f)
        {
            EntityCommandBuffer entityCommandBuffer = EndSimulationEntityCommandBufferSystem.CreateCommandBuffer(); // okay to use same commandBuffer if operations happen in the same frame (I think >_>)
            foreach (PlayerInfo player in onlineNearbyPlayers)
            {
                ScrollingCombatTextMessage.Create(
                EntityManager,
                EndSimulationEntityCommandBufferSystem.CreateCommandBuffer(),
                _assetGuid,
                _battlePosition,
                _green,
                player.CharEntity,
                countdown,
                _battleSCT,
                player.UserEntity
                );
            }

            --countdown;
            yield return _secondDelay;
        }

        EnableAggro(PlayerBattleFamiliars[steamIdOne]);
        EnableAggro(PlayerBattleFamiliars[steamIdTwo]);

        Core.StartCoroutine(MatchTimeoutRoutine(matchPair));
    }
    public static IEnumerator MatchTimeoutRoutine((ulong, ulong) matchPair)
    {
        yield return _timeoutDelay;

        if (!MatchPairs.Contains(matchPair)) yield break;
        Core.Log.LogInfo($"Match timeout reached, invoking HandleMatchTimeout...");

        bool remainingOne = PlayerBattleFamiliars[matchPair.Item1].Any();
        bool remainingTwo = PlayerBattleFamiliars[matchPair.Item2].Any();

        if (remainingOne && remainingTwo && MatchPairs.Contains(matchPair))
        {
            HandleMatchTimeout(matchPair);
        }
    }
    public static IEnumerator ChallengeExpirationRoutine((ulong, ulong) matchPair)
    {
        yield return _challengeExpiration;

        if (!MatchPairs.Contains(matchPair) && EmoteSystemPatch.BattleChallenges.Contains(matchPair))
        {
            EmoteSystemPatch.BattleChallenges.Remove(matchPair);
            NotifyBothPlayers(matchPair.Item1, matchPair.Item2, "Challenge expired...");
        }
    }
    static bool VerifyEligible(ulong playerOne, ulong playerTwo)
    {
        if (QueuedPlayers.Contains(playerOne) || QueuedPlayers.Contains(playerTwo))
        {
            string message = "One or both players are already queued!";
            NotifyPlayer(playerOne, message);

            return false;
        }
        else if (playerOne.TryGetFamiliarBattleGroup(out var battleGroupOne) && playerTwo.TryGetFamiliarBattleGroup(out var battleGroupTwo))
        {
            // Check the size of both groups
            bool groupOneValid = battleGroupOne.Count >= 3;
            bool groupTwoValid = battleGroupTwo.Count >= 3;

            foreach (int entry in battleGroupOne)
            {
                if (entry == 0)
                {
                    groupOneValid = false;
                    break;
                }
            }

            foreach (int entry in battleGroupTwo)
            {
                if (entry == 0)
                {
                    groupTwoValid = false;
                    break;
                }
            }

            if (!groupOneValid || !groupTwoValid)
            {
                string message = !groupOneValid && !groupTwoValid
                    ? $"Both players have less than <color=white>{TEAM_SIZE}</color> familiars in their battle groups!"
                    : !groupOneValid
                        ? $"Player One has less than <color=white>{TEAM_SIZE}</color> familiars in their battle group!"
                        : $"Player Two has less than <color=white>{TEAM_SIZE}</color> familiars in their battle group!";
                NotifyPlayer(playerOne, message);

                return false;
            }
            else
            {
                string battleGroupOneString = string.Join(", ", battleGroupOne);
                string battleGroupTwoString = string.Join(", ", battleGroupTwo);

                QueuedBattleGroups[playerOne] = battleGroupOne.Select(x => new PrefabGUID(x)).ToList();
                QueuedBattleGroups[playerTwo] = battleGroupTwo.Select(x => new PrefabGUID(x)).ToList();

                Core.Log.LogInfo($"Battle groups verified and added to queue: {battleGroupOneString} | {battleGroupTwoString}");

                return true;
            }
        }
        else
        {
            string message = "Couldn't find battle group for one or both players!";
            NotifyBothPlayers(playerOne, playerTwo, message);

            return false;
        }
    }
    public static (int position, TimeSpan timeRemaining) GetQueuePositionAndTime(ulong steamId)
    {
        int pendingCancels = 0;

        if (CancelledPairs.Any())
        {
            foreach (var cancelledPair in CancelledPairs)
            {
                if (cancelledPair.Item1 == steamId || cancelledPair.Item2 == steamId)
                {
                    pendingCancels++;
                }
            }
        }

        int index = 0;

        foreach (var match in MatchQueue)
        {
            index++;

            if (match.Item1 == steamId || match.Item2 == steamId)
            {
                // handle pending cancellations
                if (pendingCancels > 0)
                {
                    pendingCancels--;
                    continue;
                }

                // Increment the index

                // Calculate remaining time
                DateTime now = DateTime.UtcNow;

                // Remaining time for the current loop
                TimeSpan currentLoopRemaining = _matchPendingStart.AddSeconds(BATTLE_INTERVAL) - now;
                if (currentLoopRemaining < TimeSpan.Zero)
                {
                    currentLoopRemaining = TimeSpan.Zero; // Ensure no negative values
                }

                // Additional wait time based on position in queue
                TimeSpan additionalWaitTime = TimeSpan.FromSeconds((index) * BATTLE_INTERVAL);
                TimeSpan totalTimeRemaining = currentLoopRemaining + additionalWaitTime;

                return (index, totalTimeRemaining);
            }
        }

        // If the player is not in the queue
        return (0, TimeSpan.Zero);
    }
    public static void CancelAndRemovePairFromQueue((ulong, ulong) matchPair)
    {
        QueuedPlayers.Remove(matchPair.Item1);
        QueuedPlayers.Remove(matchPair.Item2);

        QueuedBattleGroups.TryRemove(matchPair.Item1, out _);
        QueuedBattleGroups.TryRemove(matchPair.Item2, out _);

        MatchPairs.Remove(matchPair);
        CancelledPairs.Add(matchPair);
    }
    static void EnableAggro(List<Entity> familiars)
    {
        foreach (Entity familiar in familiars)
        {
            if (familiar.Has<AggroConsumer>())
            {
                familiar.With((ref AggroConsumer aggroConsumer) =>
                {
                    aggroConsumer.Active._Value = true;
                });
            }

            if (familiar.Has<Aggroable>())
            {
                familiar.With((ref Aggroable aggroable) =>
                {
                    aggroable.Value._Value = true;
                });
            }
        }
    }
    public static void NotifyBothPlayers(ulong playerOne, ulong playerTwo, string message)
    {
        if (playerOne.TryGetPlayerInfo(out PlayerInfo playerInfo))
        {
            LocalizationService.HandleServerReply(EntityManager, playerInfo.User, message);
        }

        if (playerTwo.TryGetPlayerInfo(out playerInfo))
        {
            LocalizationService.HandleServerReply(EntityManager, playerInfo.User, message);
        }
    }
    static void NotifyPlayer(ulong steamId, string message)
    {
        if (steamId.TryGetPlayerInfo(out PlayerInfo playerInfo) && playerInfo.User.IsConnected)
        {
            LocalizationService.HandleServerReply(EntityManager, playerInfo.User, message);
        }
    }
    static void GenerateBattleFormations(float3 battleCenter)
    {
        PlayerOneFamiliarPositions.Clear();
        PlayerTwoFamiliarPositions.Clear();

        // Generate positions for Team One and Team Two
        for (int column = 0; column < 3; column++) // Three units per team
        {
            // Calculate positions for Team One
            float3 positionOne = battleCenter + new float3((column - 1) * UNIT_SPACING, 0, -TEAM_DISTANCE);
            PlayerOneFamiliarPositions.Add(positionOne);

            // Calculate positions for Team Two
            float3 positionTwo = battleCenter + new float3((column - 1) * UNIT_SPACING, 0, TEAM_DISTANCE);
            PlayerTwoFamiliarPositions.Add(positionTwo);
        }
    }
}
