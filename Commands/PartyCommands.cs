using Bloodcraft.Services;
using VampireCommandFramework;
using static Bloodcraft.Services.DataService.PlayerDictionaries;
using static Bloodcraft.Utilities.Misc;
using static Bloodcraft.Utilities.Misc.PlayerBoolsManager;
using static Bloodcraft.Utilities.PartyUtilities;

namespace Bloodcraft.Commands;

[CommandGroup(name: "party")]
internal static class PartyCommands
{
    [Command(name: "toggleinvites", shortHand: "inv", adminOnly: false, usage: ".party inv", description: "Toggles being able to be invited to parties, prevents damage and share exp.")]
    public static void TogglePartyInvitesCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.PlayerParties)
        {
            LocalizationService.HandleReply(ctx, "Parties are not enabled.");
            return;
        }

        string name = ctx.Event.User.CharacterName.Value;

        if (_playerParties.Any(kvp => kvp.Value.Contains(name)))
        {
            LocalizationService.HandleReply(ctx, "You are already in a party. Leave or disband it before enabling invites.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;

        TogglePlayerBool(steamId, "Grouping");
        LocalizationService.HandleReply(ctx, $"Party invites {(GetPlayerBool(steamId, "Grouping") ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
    }

    [Command(name: "add", shortHand: "a", adminOnly: false, usage: ".party a [Player]", description: "Adds player to party.")]
    public static void PartyAddCommand(ChatCommandContext ctx, string name)
    {
        if (!ConfigService.PlayerParties)
        {
            LocalizationService.HandleReply(ctx, "Parties are not enabled.");
            return;
        }

        ulong ownerId = ctx.Event.User.PlatformId;
        HandlePartyAdd(ctx, ownerId, name);
    }

    [Command(name: "remove", shortHand: "r", adminOnly: false, usage: ".party r [Player]", description: "Removes player from party.")]
    public static void PartyRemoveCommand(ChatCommandContext ctx, string name)
    {
        if (!ConfigService.PlayerParties)
        {
            LocalizationService.HandleReply(ctx, "Parties are not enabled.");
            return;
        }

        ulong ownerId = ctx.Event.User.PlatformId;

        if (!_playerParties.ContainsKey(ownerId))
        {
            LocalizationService.HandleReply(ctx, "You don't have a party.");
            return;
        }

        ConcurrentList<string> party = _playerParties[ownerId]; // check size and if player is already present in group before adding
        RemovePlayerFromParty(ctx, party, name);
    }

    [Command(name: "listmembers", shortHand: "lm", adminOnly: false, usage: ".party lm", description: "Lists party members of your active party.")]
    public static void PartyMembersCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.PlayerParties)
        {
            LocalizationService.HandleReply(ctx, "Parties are not enabled.");
            return;
        }

        ListPartyMembers(ctx, _playerParties);
    }

    [Command(name: "disband", shortHand: "end", adminOnly: false, usage: ".party end", description: "Disbands party.")]
    public static void DisbandPartyCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.PlayerParties)
        {
            LocalizationService.HandleReply(ctx, "Parties are not enabled.");
            return;
        }

        ulong ownerId = ctx.Event.User.PlatformId;

        if (!_playerParties.ContainsKey(ownerId))
        {
            LocalizationService.HandleReply(ctx, "You don't have a party to disband.");
            return;
        }

        _playerParties.TryRemove(ownerId, out _);
        // SavePlayerParties();
        LocalizationService.HandleReply(ctx, "Party disbanded.");
    }

    [Command(name: "leave", shortHand: "drop", adminOnly: false, usage: ".party drop", description: "Leaves party if in one.")]
    public static void LeavePartyCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.PlayerParties)
        {
            LocalizationService.HandleReply(ctx, "Parties are not enabled.");
            return;
        }

        ulong ownerId = ctx.Event.User.PlatformId;
        string playerName = ctx.Event.User.CharacterName.Value;

        if (_playerParties.ContainsKey(ownerId))
        {
            LocalizationService.HandleReply(ctx, "You can't leave your own party. Disband it instead.");
            return;
        }

        ConcurrentList<string> party = _playerParties.Values.FirstOrDefault(set => set.Contains(playerName));

        if (party != null)
        {
            RemovePlayerFromParty(ctx, party, playerName);
        }
        else
        {
            LocalizationService.HandleReply(ctx, "You're not in a party.");
        }
    }

    [Command(name: "reset", shortHand: "r", adminOnly: false, usage: ".party r", description: "Removes a player from all parties they are in and disbands any party they own.")]
    public static void ResetPartyCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.PlayerParties)
        {
            LocalizationService.HandleReply(ctx, "Parties are not enabled.");
            return;
        }

        ulong steamId = ctx.User.PlatformId;
        string playerName = ctx.User.CharacterName.Value;

        bool ownedParty = false;

        // Check if the player owns a party and disband it
        if (_playerParties.ContainsKey(steamId))
        {
            _playerParties.TryRemove(steamId, out _);
            ownedParty = true;
        }

        // Remove the player from all parties they might be a member of
        List<ulong> owners = [.. _playerParties.Keys];
        bool removedFromParties = false;

        foreach (var ownerId in owners)
        {
            ConcurrentList<string> party = _playerParties[ownerId];

            if (party.Contains(playerName))
            {
                party.Remove(playerName);
                removedFromParties = true;

                ownerId.SetPlayerParties(party);
            }
        }

        if (removedFromParties && ownedParty)
        {
            LocalizationService.HandleReply(ctx, $"Removed from all parties and disbanded owned party.");
        }
        else if (removedFromParties)
        {
            LocalizationService.HandleReply(ctx, $"Removed from all parties.");
        }
        else if (ownedParty)
        {
            LocalizationService.HandleReply(ctx, $"Disbanded owned party.");
        }
        else
        {
            LocalizationService.HandleReply(ctx, $"No parties found that you own or are a member in.");
        }

        // SavePlayerParties();
    }
}
