using Bloodcraft.Services;
using VampireCommandFramework;
using static Bloodcraft.Services.PlayerService;
using static Bloodcraft.Utilities.Misc;

namespace Bloodcraft.Utilities;
internal static class PartyUtilities
{
    public static void HandlePartyAdd(ChatCommandContext ctx, ulong ownerId, string playerName)
    {
        PlayerInfo playerInfo = PlayerCache.FirstOrDefault(kvp => kvp.Key.ToLower() == playerName.ToLower()).Value;

        if (playerInfo.UserEntity.Exists())
        {
            if (playerInfo.User.PlatformId == ownerId)
            {
                LocalizationService.HandleReply(ctx, "Can't add yourself to your own party!");
                return;
            }

            playerName = playerInfo.User.CharacterName.Value;

            if (InvitesEnabled(playerInfo.User.PlatformId))
            {
                AddPlayerToParty(ctx, ownerId, playerInfo);
            }
            else
            {
                LocalizationService.HandleReply(ctx, $"<color=green>{playerName}</color> does not have party invites enabled.");
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Couldn't find player...");
        }
    }
    static bool InvitesEnabled(ulong steamId)
    {
        if (GetPlayerBool(steamId, "Grouping"))
        {
            SetPlayerBool(steamId, "Grouping", false);

            return true;
        }

        return false;
    }
    static void AddPlayerToParty(ChatCommandContext ctx, ulong ownerId, PlayerInfo playerInfo)
    {
        string ownerName = ctx.Event.User.CharacterName.Value;
        string playerName = playerInfo.User.CharacterName.Value;

        // Check if the player is already in a party or owns a party
        KeyValuePair<ulong, HashSet<string>> existingPartyEntry = DataService.PlayerDictionaries.playerParties.FirstOrDefault(entry => entry.Value.Contains(playerName));

        if (existingPartyEntry.Value != null || DataService.PlayerDictionaries.playerParties.ContainsKey(playerInfo.User.PlatformId))
        {
            LocalizationService.HandleReply(ctx, $"<color=green>{playerName}</color> is already in or leading another party!");
            return;
        }

        if (!ownerId.TryGetPlayerParties(out HashSet<string> party) || party == null)
        {
            ownerId.SetPlayerParties([ownerName]);
        }

        if (CanAddPlayerToParty(party, playerName))
        {
            party.Add(playerName);
            ownerId.SetPlayerParties(party);

            LocalizationService.HandleReply(ctx, $"<color=green>{playerName}</color> added to party!");
        }
        else if (party.Count == ConfigService.MaxPartySize)
        {
            LocalizationService.HandleReply(ctx, $"Party is full, can't add <color=green>{playerName}</color>.");
        }
        else if (party.Contains(playerName))
        {
            LocalizationService.HandleReply(ctx, $"<color=green>{playerName}</color> is already in the party.");
        }
        else
        {
            LocalizationService.HandleReply(ctx, $"Couldn't add <color=green>{playerName}</color> to party...");
        }
    }
    static bool CanAddPlayerToParty(HashSet<string> party, string playerName)
    {
        return party.Count < ConfigService.MaxPartySize && !party.Contains(playerName);
    }
    public static void RemovePlayerFromParty(ChatCommandContext ctx, HashSet<string> party, string playerName)
    {
        ulong steamId = ctx.Event.User.PlatformId;

        PlayerInfo playerInfo = PlayerCache.FirstOrDefault(kvp => kvp.Key.ToLower() == playerName.ToLower()).Value;
        if (playerInfo.UserEntity.Exists() && party.Contains(playerInfo.User.CharacterName.Value))
        {
            party.Remove(playerInfo.User.CharacterName.Value);
            steamId.SetPlayerParties(party);

            LocalizationService.HandleReply(ctx, $"<color=green>{char.ToUpper(playerName[0]) + playerName[1..].ToLower()}</color> removed from party!");
        }
        else
        {
            LocalizationService.HandleReply(ctx, $"<color=green>{char.ToUpper(playerName[0]) + playerName[1..].ToLower()}</color> not found in party to remove...");
        }
    }
    public static void ListPartyMembers(ChatCommandContext ctx, Dictionary<ulong, HashSet<string>> playerParties)
    {
        ulong ownerId = ctx.Event.User.PlatformId;
        string playerName = ctx.Event.User.CharacterName.Value;

        HashSet<string> members = playerParties.ContainsKey(ownerId) ? playerParties[ownerId] : playerParties.Where(groupEntry => groupEntry.Value.Contains(playerName)).SelectMany(groupEntry => groupEntry.Value).ToHashSet();
        string replyMessage = members.Count > 0 ? string.Join(", ", members.Select(member => $"<color=green>{member}</color>")) : "No members in party.";

        LocalizationService.HandleReply(ctx, replyMessage);
    }
}

