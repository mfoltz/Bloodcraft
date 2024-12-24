using Bloodcraft.Patches;
using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM.Scripting;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using VampireCommandFramework;
using static Bloodcraft.Services.BattleService;
using static Bloodcraft.Services.DataService.FamiliarPersistence;
using static Bloodcraft.Services.PlayerService;

namespace Bloodcraft.Commands;

internal static class TestCommands
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;

    [Command(name: "battlegroup", shortHand: "bg", adminOnly: false, usage: ".bg [1/2/3]", description: "Set active familiar to battle group slot or list group if no slot entered.")]
    public static void SetBattleGroupSlot(ChatCommandContext ctx, int slot = -1)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;

        if (Matchmaker.QueuedPlayers.Contains(steamId) && steamId.TryGetFamiliarBattleGroup(out var battleGroup))
        {
            var (position, timeRemaining) = GetQueuePositionAndTime(steamId);

            LocalizationService.HandleReply(ctx, $"You can't make changes to your battle group while queued/battle in progress! Position in queue: <color=white>{position}</color> (<color=yellow>{Misc.FormatTimespan(timeRemaining)}</color>)");
            Familiars.HandleBattleGroupDetailsReply(ctx, steamId, battleGroup);

            return;
        }
        else if (slot == -1 && steamId.TryGetFamiliarBattleGroup(out battleGroup))
        {
            Familiars.HandleBattleGroupDetailsReply(ctx, steamId, battleGroup);

            return;
        }
        else if (slot < 1 || slot > 3)
        {
            LocalizationService.HandleReply(ctx, $"Please choose from 1-{TEAM_SIZE}.");

            return;
        }

        int slotIndex = --slot;

        if (steamId.TryGetFamiliarActives(out var actives) && !actives.FamKey.Equals(0) && steamId.TryGetFamiliarBattleGroup(out battleGroup))
        {
            Familiars.HandleBattleGroupAddAndReply(ctx, steamId, battleGroup, actives, slotIndex);
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Couldn't find active familiar to add to battle group!");
        }
    }

    [Command(name: "queuetest", shortHand: "qt", adminOnly: true, usage: ".qt [PlayerOne] [PlayerTwo]", description: "Queue testing.")]
    public static void QueuePlayersCommand(ChatCommandContext ctx, string playerOne, string playerTwo)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        PlayerInfo playerOneInfo = GetPlayerInfo(playerOne);
        if (!playerOneInfo.UserEntity.Exists())
        {
            ctx.Reply($"Couldn't find player one...");
            return;
        }

        PlayerInfo playerTwoInfo = GetPlayerInfo(playerTwo);
        if (!playerTwoInfo.UserEntity.Exists())
        {
            ctx.Reply($"Couldn't find player two...");
            return;
        }

        if (GenerateRandomBattleGroup(ctx, playerOneInfo) && GenerateRandomBattleGroup(ctx, playerTwoInfo))
        {
            ctx.Reply($"Queuing <color=white>{playerOneInfo.User.CharacterName.Value}</color> and <color=white>{playerTwoInfo.User.CharacterName.Value}</color> for a battle!");
            Matchmaker.QueueMatch((playerOneInfo.User.PlatformId, playerTwoInfo.User.PlatformId));
        }
    }

    [Command(name: "forcechallenge", shortHand: "fc", adminOnly: true, usage: ".fc [PlayerName]", description: "Challenge testing.")]
    public static void ForceChallengeCommand(ChatCommandContext ctx, string name)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;

        PlayerInfo playerInfo = GetPlayerInfo(name);
        if (!playerInfo.UserEntity.Exists())
        {
            ctx.Reply($"Couldn't find player.");
            return;
        }

        if (GenerateRandomBattleGroup(ctx, playerInfo))
        {
            Matchmaker.QueueMatch((steamId, playerInfo.User.PlatformId));
        }
    }

    [Command(name: "challenge", adminOnly: false, usage: ".challenge [PlayerName/cancel]", description: "Challenges player if found, use cancel to exit queue.")]
    public static void ChallengePlayerCommand(ChatCommandContext ctx, string name)
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;

        if (name.ToLower() == "cancel")
        {
            foreach (var matchPairs in Matchmaker.MatchPairs)
            {
                if (SpawnTransformSystemOnSpawnPatch.PlayerBattleFamiliars.TryGetValue(steamId, out List<Entity> familiarsInBattle) && familiarsInBattle.Count > 0)
                {
                    ctx.Reply("Can't cancel challenge until battle is over!");
                    return;
                }
                else if (matchPairs.Item1 == steamId || matchPairs.Item2 == steamId)
                {
                    NotifyBothPlayers(matchPairs.Item1, matchPairs.Item2, "Challenge cancelled, removed from queue...");
                    CancelAndRemovePairFromQueue(matchPairs);
                    return;
                }
            }

            ctx.Reply("You're not currently queued for a battle!");
            return;
        }

        PlayerInfo playerInfo = GetPlayerInfo(name);
        if (!playerInfo.UserEntity.Exists())
        {
            ctx.Reply($"Couldn't find player.");
            return;
        }

        if (playerInfo.User.PlatformId == steamId)
        {
            ctx.Reply("You can't challenge yourself!");
            return;
        }

        foreach (var challenge in EmoteSystemPatch.BattleChallenges)
        {
            if (challenge.Item1 == steamId || challenge.Item2 == steamId)
            {
                ctx.Reply("Can't challenge another player until current one expires!");
                return;
            }
        }

        EmoteSystemPatch.BattleChallenges.Add((ctx.User.PlatformId, playerInfo.User.PlatformId));

        ctx.Reply($"Challenged <color=white>{playerInfo.User.CharacterName.Value}</color> to a battle! (<color=yellow>30s</color> until it expires)");
        LocalizationService.HandleServerReply(EntityManager, playerInfo.User, $"<color=white>{ctx.User.CharacterName.Value}</color> has challenged you to a battle! (<color=yellow>30s</color> until it expires, accept by emoting '<color=green>Yes</color>' or decline by emoting '<color=red>No</color>')");

        Core.StartCoroutine(ChallengeExpiredRoutine((ctx.User.PlatformId, playerInfo.User.PlatformId)));
    }

    [Command(name: "setfamiliarbattlearena", shortHand: "sfba", adminOnly: true, usage: ".sfba", description: "Designate current position as the center used for the familiar arena.")]
    public static void SetBattleArenaCoords(ChatCommandContext ctx) // groups aligned with north-south axis
    {
        if (!ConfigService.FamiliarSystem)
        {
            LocalizationService.HandleReply(ctx, "Familiars are not enabled.");
            return;
        }

        Entity character = ctx.Event.SenderCharacterEntity;

        float3 location = character.ReadRO<Translation>().Value;
        List<float> floats = [location.x, location.y, location.z];

        DataService.PlayerDictionaries._familiarBattleCoords.Clear();
        DataService.PlayerDictionaries._familiarBattleCoords.Add(floats);
        DataService.PlayerPersistence.SaveFamiliarBattleCoords();

        if (_battlePosition.Equals(float3.zero))
        {
            Initialize();
            LocalizationService.HandleReply(ctx, "Familiar battle arena position set, battle service started! (one allowed)");
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Familiar battle arena position set (one allowed, previous coords overwritten).");
        }
    }
    static bool GenerateRandomBattleGroup(ChatCommandContext ctx, PlayerInfo playerInfo)
    {
        ulong steamId = playerInfo.User.PlatformId;
        UnlockedFamiliarData unlockedFamiliarData = FamiliarUnlocksManager.LoadUnlockedFamiliars(steamId);

        if (!steamId.TryGetFamiliarBattleGroup(out var battleGroup))
        {
            battleGroup = [0, 0, 0]; // Initialize battle group if not found
            steamId.SetFamiliarBattleGroup(battleGroup);
        }

        System.Random random = new();
        var unlocks = unlockedFamiliarData.UnlockedFamiliars.Values
                                          .Where(list => list.Count > 0)
                                          .ToList();

        FamiliarExperienceData xpData = FamiliarExperienceManager.LoadFamiliarExperience(steamId);

        // Gather all eligible familiars with level >= 25
        var eligibleFamiliars = new List<(int famKey, int level)>();
        foreach (var familiarList in unlocks)
        {
            foreach (var famKey in familiarList)
            {
                if (xpData.FamiliarExperience.TryGetValue(famKey, out var xpDataPair) && xpDataPair.Key >= 25)
                {
                    eligibleFamiliars.Add((famKey, xpDataPair.Key));
                }
            }
        }

        if (eligibleFamiliars.Count < 3)
        {
            ctx.Reply($"Not enough level 25+ familiars to generate a battle group for <color=white>{playerInfo.User.CharacterName.Value}</color>...");
            return false;
        }

        // Select 3 familiars with pseudo-randomness
        var selectedFamiliars = eligibleFamiliars.OrderBy(x => random.Next()) // Shuffle for randomness
                                                 .Take(3)
                                                 .Select(familiar => familiar.famKey)
                                                 .ToList();

        for (int i = 0; i < 3; i++)
        {
            battleGroup[i] = selectedFamiliars[i];
        }

        ctx.Reply($"Random battle group successfully generated for <color=white>{playerInfo.User.CharacterName.Value}</color>!");

        // Save the generated battle group
        steamId.SetFamiliarBattleGroup(battleGroup);
        Familiars.HandleBattleGroupDetailsReply(ctx, steamId, battleGroup);

        return true;
    }
}
