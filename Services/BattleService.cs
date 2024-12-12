using Bloodcraft.Patches;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Utilities;
using Stunlock.Core;
using System.Collections;
using System.Collections.Concurrent;
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

    public static readonly List<float3> FamiliarBattleCoords = []; // one for now but leaving as list for potential expansion later

    public static readonly List<float3> PlayerOneFamiliarPositions = [];
    public static readonly List<float3> PlayerTwoFamiliarPositions = [];

    static readonly WaitForSeconds BattleInterval = new(BATTLE_INTERVAL);
    static readonly WaitForSeconds TimeoutDelay = new(FamiliarSummonSystem.FAMILIAR_LIFETIME);
    static readonly WaitForSeconds ChallengeExpiration = new(CHALLENGE_EXPIRATION);

    const float BATTLE_INTERVAL = 300f;
    const float CHALLENGE_EXPIRATION = 30f;
    const float UNIT_SPACING = 2.5f;
    const float TEAM_DISTANCE = 2.5f;
    public const int BATTLE_SIZE = 3;

    static DateTime _matchPendingStart;
    static bool _serviceActive = false;
    static bool _matchPending = false;
    public BattleService()
    {
        Initialize();
    }
    public static void Initialize()
    {
        if (familiarBattleCoords.Any())
        {
            List<float> floats = familiarBattleCoords.FirstOrDefault();
            float3 position = new(floats[0], floats[1], floats[2]);
            FamiliarBattleCoords.Add(position);
            float3 battlePosition = FamiliarBattleCoords.FirstOrDefault();

            BattlePosition = new(battlePosition.x, battlePosition.y, battlePosition.z);
            SCTPosition = new(battlePosition.x, battlePosition.y + 5f, battlePosition.z);

            GenerateBattleFormations(battlePosition);
            Core.StartCoroutine(BattleRoutine());

            _serviceActive = true;
        }
    }
    public static class Matchmaker // announce matches and stuff to people inside a radius around the set position?
    {
        public static readonly ConcurrentQueue<(ulong, ulong)> MatchQueue = new();
        public static readonly HashSet<(ulong, ulong)> MatchPairs = [];
        public static readonly HashSet<ulong> QueuedPlayers = [];
        public static readonly ConcurrentDictionary<ulong, List<PrefabGUID>> QueuedBattleGroups = new();
        public static readonly HashSet<(ulong, ulong)> CancelledPairs = [];

        // Add a pair of players to the queue
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

        // End a match and unblock players
        public static void HandleMatchCompletion((ulong, ulong) matchPair, ulong winner)
        {
            QueuedPlayers.Remove(matchPair.Item1);
            QueuedPlayers.Remove(matchPair.Item2);
            MatchPairs.Remove(matchPair);

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

                NotifyBothPlayers(matchPair.Item1, matchPair.Item2, $"{formattedLoserName} familiars have been defeated! Winner: <color=#FFD700>{winnerInfo.User.CharacterName.Value}</color>");
            }
        }
        public static void HandleMatchTimeout((ulong, ulong) matchPair)
        {
            QueuedPlayers.Remove(matchPair.Item1);
            QueuedPlayers.Remove(matchPair.Item2);
            MatchPairs.Remove(matchPair);

            int playerOneFamiliars = PlayerBattleFamiliars[matchPair.Item1].Count;
            int playerTwoFamiliars = PlayerBattleFamiliars[matchPair.Item2].Count;

            ulong winner = playerOneFamiliars > playerTwoFamiliars
                ? matchPair.Item1
                : playerTwoFamiliars > playerOneFamiliars
                    ? matchPair.Item2
                    : 0;

            if (matchPair.TryGetMatchPairInfo(out (PlayerInfo, PlayerInfo) matchPairInfo) && winner != 0)
            {
                // other logic after match ends, inform players of outcome handle rewards or whatever
                PlayerInfo winnerInfo = winner == matchPairInfo.Item1.User.PlatformId ? matchPairInfo.Item1 : matchPairInfo.Item2;
                PlayerInfo loserInfo = winner == matchPairInfo.Item1.User.PlatformId ? matchPairInfo.Item2 : matchPairInfo.Item1;

                string loserName = loserInfo.User.CharacterName.Value;
                string formattedLoserName = $"<color=#808080>{loserName}</color>";

                NotifyBothPlayers(matchPair.Item1, matchPair.Item2, $"{formattedLoserName} has the fewest familiars remaining! Winner: <color=#FFD700>{winnerInfo.User.CharacterName.Value}</color>");
            }
            else
            {
                NotifyBothPlayers(matchPair.Item1, matchPair.Item2, "Match timeout, result is a tie! Better luck next time.");
            }

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

            yield return BattleInterval;

            Core.Log.LogInfo("Checking for pending matches in queue...");
            if (MatchQueue.TryDequeue(out var match))
            {
                Core.Log.LogInfo("Starting match from queue...");

                // Check if the match is in the cancelled pairs
                if (CancelledPairs.Contains(match))
                {
                    // Skip this match
                    continue;
                }

                ulong playerOne = match.Item1;
                ulong playerTwo = match.Item2;

                // Validate the players
                if (playerOne.TryGetPlayerInfo(out PlayerInfo playerOneInfo) &&
                    playerTwo.TryGetPlayerInfo(out PlayerInfo playerTwoInfo))
                {
                    // Found a valid match
                    Core.Log.LogInfo("PlayerInfo acquired, invoking HandleBattleSummoning()...");

                    SetRotation.Add(playerTwo);
                    HandleBattleSummoning(playerOneInfo, playerTwoInfo, playerOne, playerTwo);
                    _matchPending = false;
                }
            }
            else
            {
                Core.Log.LogInfo("No matches in queue/couldn't pop match...");
                _matchPending = false;
            }
        }
    }
    static IEnumerator BattleSummoningRoutine(Entity playerOne, Entity playerUserOne, Entity playerTwo, Entity playerUserTwo,
    List<PrefabGUID> playerOneFamiliars, List<PrefabGUID> playerTwoFamiliars)
    {
        Core.Log.LogInfo($"PlayerOneFams: {playerOneFamiliars.Count} | PlayerTwoFams: {playerTwoFamiliars.Count} | FormationOne: {PlayerOneFamiliarPositions.Count} | FormationTwo: {PlayerTwoFamiliarPositions.Count}");

        for (int i = 0; i < BATTLE_SIZE; i++)
        {
            FamiliarSummonSystem.SummonFamiliarForBattle(playerOne, playerUserOne, playerOneFamiliars[i], PlayerOneFamiliarPositions[i]);
            FamiliarSummonSystem.SummonFamiliarForBattle(playerTwo, playerUserTwo, playerTwoFamiliars[i], PlayerTwoFamiliarPositions[i]);

            // Yield after summoning a pair
            yield return null;
        }
    }
    static void HandleBattleSummoning(PlayerInfo playerOneInfo, PlayerInfo playerTwoInfo, ulong playerOne, ulong playerTwo)
    {
        if (QueuedBattleGroups.TryRemove(playerOne, out var battleGroupOne) && QueuedBattleGroups.TryRemove(playerTwo, out var battleGroupTwo))
        {
            Core.Log.LogInfo("Battle groups popped, starting battle summon routine...");

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
    static void GenerateBattleFormations(float3 battleCenter)
    {
        PlayerOneFamiliarPositions.Clear();
        PlayerTwoFamiliarPositions.Clear();

        // Offset to separate the two teams (along the z-axis)
        float3 teamTwoOffset = new(0, 0, TEAM_DISTANCE); // Distance between teams (adjust if needed)

        // Generate positions for Team One and Team Two
        for (int column = 0; column < 3; column++) // Three units per team
        {
            // Calculate positions for Team One
            float3 positionOne = battleCenter + new float3((column - 1) * UNIT_SPACING, 0, -TEAM_DISTANCE);
            PlayerOneFamiliarPositions.Add(positionOne);

            // Calculate positions for Team Two (mirrored along the z-axis)
            float3 positionTwo = battleCenter + teamTwoOffset + new float3((column - 1) * UNIT_SPACING, 0, TEAM_DISTANCE);
            PlayerTwoFamiliarPositions.Add(positionTwo);
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
                    ? $"Both players have less than <color=white>{BATTLE_SIZE}</color> familiars in their battle groups!"
                    : !groupOneValid
                        ? $"Player One has less than <color=white>{BATTLE_SIZE}</color> familiars in their battle group!"
                        : $"Player Two has less than <color=white>{BATTLE_SIZE}</color> familiars in their battle group!";
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
    public static void NotifyBothPlayers(ulong playerOne, ulong playerTwo, string message)
    {
        if (playerOne.TryGetPlayerInfo(out PlayerInfo playerInfo) && playerInfo.User.IsConnected)
        {
            LocalizationService.HandleServerReply(EntityManager, playerInfo.User, message);
        }

        if (playerTwo.TryGetPlayerInfo(out playerInfo) && playerInfo.User.IsConnected)
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
    public static (int position, TimeSpan timeRemaining) GetQueuePositionAndTime(ulong steamId)
    {
        // Find the player's match in the queue
        int index = 0;
        foreach (var match in MatchQueue)
        {
            index++;
            if (match.Item1 == steamId || match.Item2 == steamId)
            {
                // Calculate remaining time
                DateTime now = DateTime.UtcNow;

                // Remaining time for the current loop
                TimeSpan currentLoopRemaining = _matchPendingStart.AddSeconds(BATTLE_INTERVAL) - now;
                if (currentLoopRemaining < TimeSpan.Zero)
                {
                    currentLoopRemaining = TimeSpan.Zero; // Ensure no negative values
                }

                // Additional wait time based on position in queue
                TimeSpan additionalWaitTime = TimeSpan.FromSeconds((index - 1) * BATTLE_INTERVAL);
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
    public static IEnumerator MatchTimeoutRoutine((ulong, ulong) matchPair)
    {
        yield return TimeoutDelay;

        if (MatchPairs.Contains(matchPair))
        {
            HandleMatchTimeout(matchPair);
        }
    }
    public static IEnumerator ChallengeExpirationRoutine((ulong, ulong) matchPair)
    {
        yield return ChallengeExpiration;

        if (!MatchPairs.Contains(matchPair))
        {
            if (EmoteSystemPatch.BattleChallenges.Contains(matchPair))
            {
                EmoteSystemPatch.BattleChallenges.Remove(matchPair);
                NotifyBothPlayers(matchPair.Item1, matchPair.Item2, "Challenge expired...");
            }
        }
    }
}
