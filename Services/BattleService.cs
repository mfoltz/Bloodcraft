using Bloodcraft.Patches;
using Bloodcraft.Utilities;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static Bloodcraft.Services.BattleService.Matchmaker;
using static Bloodcraft.Services.DataService.FamiliarPersistence;
using static Bloodcraft.Services.DataService.PlayerDictionaries;
using static Bloodcraft.Services.PlayerService;
using static Bloodcraft.Systems.Familiars.FamiliarBindingSystem;

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
    static readonly WaitForSeconds _timeoutDelay = new(FAMILIAR_LIFETIME - MATCH_START_COUNTDOWN);

    static readonly WaitForSeconds _secondDelay = new(1f);
    static readonly WaitForSeconds _delay = new(0.25f);

    /*
    static readonly ComponentType[] _unitTeamComponent =
    [
        ComponentType.ReadOnly(Il2CppType.Of<UnitTeam>()),
    ];
    */

    static ComponentType[] _unitTeamComponent =
    [
        ComponentType.ReadOnly(Il2CppType.Of<UnitTeam>()),
    ];

    static readonly AssetGuid _valueSecondsAssetGuid = AssetGuid.FromString("2a1f5c1b-5a50-4ff0-a982-ca37efb8f69d");
    static readonly PrefabGUID _sctInfoWarning = new(106212079);
    static readonly float3 _green = new(0f, 1f, 0f);

    public static float3 _battlePosition = float3.zero;
    public static float3 _sctPosition = float3.zero;

    const float MATCH_START_COUNTDOWN = 5f;
    const float BATTLE_INTERVAL = 300f;
    const float CHALLENGE_EXPIRATION = 30f;
    const float UNIT_SPACING = 2.5f;
    const float TEAM_DISTANCE = 3f;
    const float SPECTATE_DISTANCE = 25f;
    const float SCT_HEIGHT = 15f;

    public const int TEAM_SIZE = 3;
    const int TEAM_ONE = 0;
    const int TEAM_TWO = 1;

    static DateTime _matchPendingStart;
    static bool _serviceActive = false;
    static bool _matchPending = false;

    // Sanguis reflection
    public static bool _awardSanguis = false;
    public static int _tokensTransferred;
    public static PropertyInfo _tokensProperty;
    public static MethodInfo _saveTokens;
    public static Dictionary<ulong, (int Tokens, (DateTime Start, DateTime DailyLogin) TimeData)> _playerTokens;
    public BattleService()
    {
        // Initialize();
    }
    public static void Initialize()
    {
        if (_familiarBattleCoords.Any())
        {
            List<float> floats = _familiarBattleCoords.FirstOrDefault();
            float3 battlePosition = new(floats[0], floats[1], floats[2]);
            FamiliarBattleCoords.Add(battlePosition);

            _battlePosition = new(battlePosition.x, battlePosition.y, battlePosition.z);
            _sctPosition = new(battlePosition.x, battlePosition.y + SCT_HEIGHT, battlePosition.z);

            GenerateBattleFormations(battlePosition);
            BattleUpdateRoutine().Start();

            _serviceActive = true;

            try
            {
                // Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<ComponentType> componentTypes = new(_unitTeamComponent);
                EntityQueryBuilder entityQueryBuilder = new();

                entityQueryBuilder.WithAll(ref _unitTeamComponent);
                entityQueryBuilder.WithOptions(EntityQueryOptions.IncludeDisabled);

                EntityQuery unitTeamQuery = EntityManager.CreateEntityQuery(ref entityQueryBuilder);

                /*
                EntityQuery unitTeamQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
                {
                    All = _unitTeamComponent,
                    Options = EntityQueryOptions.IncludeDisabled
                });
                */

                NativeArray<Entity> entities = unitTeamQuery.ToEntityArray(Allocator.TempJob);

                try
                {
                    foreach (Entity entity in entities)
                    {
                        if (entity.Has<UnitTeam>())
                        {
                            _unitTeamSingleton = entity;
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
        public static void QueueMatch((ulong, ulong) match)
        {
            ulong playerOne = match.Item1;
            ulong playerTwo = match.Item2;

            if (!_serviceActive)
            {
                NotifyBothPlayers(playerOne, playerTwo, "Battle service inactive, arena position hasn't been set!");
                return;
            }
            else if (!VerifyEligible(playerOne, playerTwo)) return;

            QueuedPlayers.Add(playerOne);
            QueuedPlayers.Add(playerTwo);

            (ulong, ulong) matchPair = new(playerOne, playerTwo);
            MatchQueue.Enqueue(matchPair);
            MatchPairs.Add(matchPair);

            var (position, timeRemaining) = GetQueuePositionAndTime(playerOne);
            string message = $"Queued successfully! Position in queue: <color=white>{position}</color> (<color=yellow>{Misc.FormatTimespan(timeRemaining)}</color>)";
            Core.Log.LogWarning($"{playerOne} & {playerTwo} | {message}");

            NotifyBothPlayers(playerOne, playerTwo, message);
        }
        public static void HandleMatchCompletion((ulong, ulong) matchPair, ulong winner)
        {
            QueuedPlayers.Remove(matchPair.Item1);
            QueuedPlayers.Remove(matchPair.Item2);
            MatchPairs.Remove(matchPair);

            if (PlayerBattleFamiliars.TryRemove(matchPair.Item1, out List<Entity> playerOneFamiliars)
                && PlayerBattleFamiliars.TryRemove(matchPair.Item2, out List<Entity> playerTwoFamiliars))
            {
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

                if (matchPair.TryGetMatchPairInfo(out (PlayerInfo, PlayerInfo) matchPairInfo))
                {
                    // other logic after match ends, inform players of outcome handle rewards or whatever
                    PlayerInfo winnerInfo = winner == matchPairInfo.Item1.User.PlatformId ? matchPairInfo.Item1 : matchPairInfo.Item2;
                    PlayerInfo loserInfo = winner == matchPairInfo.Item1.User.PlatformId ? matchPairInfo.Item2 : matchPairInfo.Item1;

                    string loserName = loserInfo.User.CharacterName.Value;
                    string loserNameWithSuffix = loserName.EndsWith("s", StringComparison.CurrentCultureIgnoreCase)
                    ? $"{loserName}’"
                    : $"{loserName}’s";
                    string formattedLoserName = $"<color=#808080>{loserNameWithSuffix}</color>";

                    NotifyBothPlayers(matchPair.Item1, matchPair.Item2, $"{formattedLoserName} familiars have been defeated! Winner: <color=#E5B800>{winnerInfo.User.CharacterName.Value}</color>");
                }
                else
                {
                    Core.Log.LogWarning("Failed to get match pair info during battle completion...");
                }
            }
            else
            {
                Core.Log.LogWarning("Failed to remove one or both players from PlayerBattleFamiliars during match completion...");
            }
        }
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

                    NotifyBothPlayers(matchPair.Item1, matchPair.Item2, $"{formattedLoserName} has the least familiars remaining! Winner: <color=#E5B800>{winnerInfo.User.CharacterName.Value}</color>");
                }
                else
                {
                    Core.Log.LogWarning("Failed to get match pair info during match timeout completion...");
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
            else
            {
                Core.Log.LogWarning("Failed to remove one or both players from PlayerBattleFamiliars during match timeout...");
            }
        }
    }
    static IEnumerator BattleUpdateRoutine()
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

                    ulong playerOne = match.Item1;
                    ulong playerTwo = match.Item2;

                    // Validate the players
                    if (playerOne.TryGetPlayerInfo(out PlayerInfo playerOneInfo) &&
                        playerTwo.TryGetPlayerInfo(out PlayerInfo playerTwoInfo))
                    {
                        // Found a valid match
                        Core.Log.LogInfo("PlayerInfo acquired, invoking HandleBattleSummoning...");

                        HandleBattleSummoning(playerOneInfo, playerTwoInfo, playerOne, playerTwo);
                        _matchPending = false;

                        break;
                    }
                    else
                    {
                        Core.Log.LogWarning("Failed to get PlayerInfo for one or both players for match start, skipping to next and removing pair from queue...");
                        RemovePairFromQueue(match);
                    }
                }
            }
            else
            {
                Core.Log.LogInfo("No pending matches in queue...");
                _matchPending = false;
            }
        }
    }
    public static IEnumerator BattleCountdownRoutine((ulong playerOne, ulong playerTwo) matchPair)
    {
        if (!matchPair.TryGetMatchPairInfo(out (PlayerInfo, PlayerInfo) matchPairInfo))
        {
            Core.Log.LogWarning("Failed to get match pair info during battle start countdown...");

            yield break;
        }

        float countdown = MATCH_START_COUNTDOWN;
        List<PlayerInfo> onlineNearbyPlayers = Progression.GetUsersNearPosition(_battlePosition, SPECTATE_DISTANCE);

        ulong steamIdOne = matchPair.playerOne;
        ulong steamIdTwo = matchPair.playerTwo;

        while (countdown > 0f)
        {
            foreach (PlayerInfo player in onlineNearbyPlayers)
            {
                ScrollingCombatTextMessage.Create(
                EntityManager,
                EndSimulationEntityCommandBufferSystem.CreateCommandBuffer(),
                _valueSecondsAssetGuid,
                _battlePosition,
                _green,
                player.CharEntity,
                countdown,
                _sctInfoWarning,
                player.UserEntity
                );
            }

            --countdown;
            yield return _secondDelay;
        }

        EnableAggro(PlayerBattleFamiliars[steamIdOne]);
        EnableAggro(PlayerBattleFamiliars[steamIdTwo]);

        MatchTimeoutRoutine(matchPair).Start();
    }
    static IEnumerator BattleSummoningRoutine(Entity playerOne, User playerUserOne, Entity playerTwo, User playerUserTwo,
    List<PrefabGUID> playerOneFamiliars, List<PrefabGUID> playerTwoFamiliars)
    {
        bool allies = playerOne.IsAllied(playerTwo);

        for (int i = 0; i < TEAM_SIZE; i++)
        {
            InstantiateFamiliarRoutine(playerUserOne, playerOne, playerOneFamiliars[i].GuidHash, true, TEAM_ONE, PlayerOneFamiliarPositions[i], allies).Start();
            InstantiateFamiliarRoutine(playerUserTwo, playerTwo, playerTwoFamiliars[i].GuidHash, true, TEAM_TWO, PlayerTwoFamiliarPositions[i], allies).Start();
            yield return _delay;
        }
    }
    public static IEnumerator MatchTimeoutRoutine((ulong, ulong) matchPair)
    {
        yield return _timeoutDelay;

        if (!MatchPairs.Contains(matchPair))
        {
            Core.Log.LogWarning("Match timeout reached, unable to retrieve active matchPair...");
            yield break;
        }

        Core.Log.LogInfo($"Match timeout reached, proceeding to HandleMatchTimeout...");

        bool remainingOne = PlayerBattleFamiliars[matchPair.Item1].Any();
        bool remainingTwo = PlayerBattleFamiliars[matchPair.Item2].Any();

        if (remainingOne && remainingTwo && MatchPairs.Contains(matchPair))
        {
            HandleMatchTimeout(matchPair);
        }
        else if ((remainingOne || remainingTwo) && MatchPairs.Contains(matchPair)) // cleanup if needed?
        {
            HandleMatchTimeout(matchPair);
        }
        else if (remainingOne || remainingTwo)
        {
            Core.Log.LogWarning("One or both player has remaining familiars but is not part of an active matchPair, cleaning up...");

            if (remainingOne && PlayerBattleFamiliars.TryRemove(matchPair.Item1, out List<Entity> playerOneFamiliars))
            {
                foreach (Entity familiar in playerOneFamiliars)
                {
                    if (LinkMinionToOwnerOnSpawnSystemPatch.FamiliarMinions.ContainsKey(familiar)) Familiars.HandleFamiliarMinions(familiar);
                    if (familiar.Exists()) familiar.Destroy();
                }
            }

            if (remainingTwo && PlayerBattleFamiliars.TryRemove(matchPair.Item2, out List<Entity> playerTwoFamiliars))
            {
                foreach (Entity familiar in playerTwoFamiliars)
                {
                    if (LinkMinionToOwnerOnSpawnSystemPatch.FamiliarMinions.ContainsKey(familiar)) Familiars.HandleFamiliarMinions(familiar);
                    if (familiar.Exists()) familiar.Destroy();
                }
            }
        }
    }
    public static IEnumerator ChallengeExpiredRoutine((ulong, ulong) matchPair)
    {
        yield return _challengeExpiration;

        if (!MatchPairs.Contains(matchPair) && EmoteSystemPatch.BattleChallenges.Contains(matchPair))
        {
            EmoteSystemPatch.BattleChallenges.Remove(matchPair);
            NotifyBothPlayers(matchPair.Item1, matchPair.Item2, "Challenge expired...");
        }
    }
    static void HandleBattleSummoning(PlayerInfo playerOneInfo, PlayerInfo playerTwoInfo, ulong playerOne, ulong playerTwo)
    {
        if (QueuedBattleGroups.TryRemove(playerOne, out var battleGroupOne) && QueuedBattleGroups.TryRemove(playerTwo, out var battleGroupTwo))
        {
            Core.Log.LogInfo("Battle groups popped, invoking BattleSummoningRoutine...");

            PlayerBattleGroups[playerOne] = battleGroupOne;
            PlayerBattleGroups[playerTwo] = battleGroupTwo;

            BattleSummoningRoutine(playerOneInfo.CharEntity, playerOneInfo.User,
                playerTwoInfo.CharEntity, playerTwoInfo.User,
                [..battleGroupOne], [..battleGroupTwo]).Start();
        }
        else
        {
            Core.Log.LogInfo("Couldn't pop battle groups for one or both players...");
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
        else
        {
            var battleGroupOne = FamiliarBattleGroupsManager.GetFamiliarBattleGroup(playerOne, FamiliarBattleGroupsManager.GetActiveBattleGroupName(playerOne));
            var battleGroupTwo = FamiliarBattleGroupsManager.GetFamiliarBattleGroup(playerTwo, FamiliarBattleGroupsManager.GetActiveBattleGroupName(playerTwo));

            bool groupOneValid = !battleGroupOne.Familiars.Contains(0);
            bool groupTwoValid = !battleGroupTwo.Familiars.Contains(0);
            bool battleGroupsValid = groupOneValid && groupTwoValid;

            if (!battleGroupsValid)
            {
                string message = !groupOneValid && !groupTwoValid
                    ? $"Both players have less than <color=white>{TEAM_SIZE}</color> familiars in their battle groups!"
                    : !groupOneValid
                        ? $"Player One has less than <color=white>{TEAM_SIZE}</color> familiars in their battle group!"
                        : $"Player Two has less than <color=white>{TEAM_SIZE}</color> familiars in their battle group!";
                NotifyPlayer(playerOne, message);
                Core.Log.LogWarning(message);

                return false;
            }
            else
            {
                string battleGroupOneString = string.Join(", ", battleGroupOne.Familiars);
                string battleGroupTwoString = string.Join(", ", battleGroupTwo.Familiars);

                QueuedBattleGroups[playerOne] = [..battleGroupOne.Familiars.Select(x => new PrefabGUID(x))];
                QueuedBattleGroups[playerTwo] = [..battleGroupTwo.Familiars.Select(x => new PrefabGUID(x))];

                Core.Log.LogInfo($"Battle groups for {playerOne} & {playerTwo} verified and added to queue: {battleGroupOneString} | {battleGroupTwoString}");

                return true;
            }
        }
    }
    public static (int position, TimeSpan timeRemaining) GetQueuePositionAndTime(ulong steamId)
    {
        int index = 0;

        foreach (var match in MatchQueue)
        {
            index++;

            if (match.Item1 == steamId || match.Item2 == steamId)
            {
                DateTime now = DateTime.UtcNow;
                TimeSpan currentLoopRemaining = _matchPendingStart.AddSeconds(BATTLE_INTERVAL) - now;

                if (currentLoopRemaining < TimeSpan.Zero)
                {
                    currentLoopRemaining = TimeSpan.Zero;
                }

                TimeSpan additionalWaitTime = TimeSpan.FromSeconds((index - 1) * BATTLE_INTERVAL);
                TimeSpan totalTimeRemaining = currentLoopRemaining + additionalWaitTime;

                return (index, totalTimeRemaining);
            }
        }

        return (0, TimeSpan.Zero);
    }
    public static void RemovePairFromQueue((ulong, ulong) matchPair)
    {
        QueuedPlayers.Remove(matchPair.Item1);
        QueuedPlayers.Remove(matchPair.Item2);

        QueuedBattleGroups.TryRemove(matchPair.Item1, out _);
        QueuedBattleGroups.TryRemove(matchPair.Item2, out _);

        MatchPairs.Remove(matchPair);
    }
    static void EnableAggro(List<Entity> familiars)
    {
        foreach (Entity familiar in familiars)
        {
            if (familiar.Has<AggroConsumer>())
            {
                familiar.With((ref AggroConsumer aggroConsumer) => aggroConsumer.Active._Value = true);
            }

            if (familiar.Has<Aggroable>())
            {
                familiar.With((ref Aggroable aggroable) => aggroable.Value._Value = true);
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

        for (int column = 0; column < 3; column++) // Three units per team
        {
            float3 positionOne = battleCenter + new float3((column - 1) * UNIT_SPACING, 0, -TEAM_DISTANCE);
            PlayerOneFamiliarPositions.Add(positionOne);

            float3 positionTwo = battleCenter + new float3((column - 1) * UNIT_SPACING, 0, TEAM_DISTANCE);
            PlayerTwoFamiliarPositions.Add(positionTwo);
        }
    }
    static void TransferTokensAndSave(ulong winner, ulong loser, int amount)
    {
        var playerTokens = (Dictionary<ulong, (int, (DateTime, DateTime))>)_tokensProperty.GetValue(null);

        if (playerTokens.TryGetValue(winner, out var winnerTokens) && playerTokens.TryGetValue(loser, out var loserTokens))
        {
            winnerTokens.Item1 += amount;
            loserTokens.Item1 -= amount;

            _saveTokens.Invoke(null, null);
            Core.Log.LogInfo($"Sanguis awarded after battle: {amount} from {loser} to {winner}");
        }
    }
}

/*
if (_awardSanguis)
{
    try
    {
        TransferTokensAndSave(winnerInfo.User.PlatformId, loserInfo.User.PlatformId, _tokensTransferred);
        NotifyBothPlayers(matchPair.Item1, matchPair.Item2, $"{formattedLoserName} familiars have been defeated! Winner: <color=#E5B800>{winnerInfo.User.CharacterName.Value}</color>");
    }
    catch (Exception ex)
    {
        Core.Log.LogError($"Error transferring tokens and saving - {ex}");
    }
}
else
{
}
*/
