using Bloodcraft.Services;
using Bloodcraft.Utilities;
using VampireCommandFramework;
using static Bloodcraft.Services.DataService.PlayerDictionaries;
using static Bloodcraft.Services.DataService.PlayerPersistence;
using static Bloodcraft.Utilities.PlayerUtilities.PartyUtilities;

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

        if (playerParties.Any(kvp => kvp.Value.Contains(name)))
        {
            LocalizationService.HandleReply(ctx, "You are already in a party. Leave or disband it before enabling invites.");
            return;
        }

        ulong SteamID = ctx.Event.User.PlatformId;

        PlayerUtilities.TogglePlayerBool(SteamID, "Grouping");
        LocalizationService.HandleReply(ctx, $"Party invites {(PlayerUtilities.GetPlayerBool(SteamID, "Grouping") ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
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

        if (!playerParties.ContainsKey(ownerId))
        {
            LocalizationService.HandleReply(ctx, "You don't have a party.");
            return;
        }

        HashSet<string> party = playerParties[ownerId]; // check size and if player is already present in group before adding
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

        ListPartyMembers(ctx, playerParties);
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

        if (!playerParties.ContainsKey(ownerId))
        {
            LocalizationService.HandleReply(ctx, "You don't have a party to disband.");
            return;
        }

        playerParties.Remove(ownerId);
        SavePlayerParties();
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

        if (playerParties.ContainsKey(ownerId))
        {
            LocalizationService.HandleReply(ctx, "You can't leave your own party. Disband it instead.");
            return;
        }

        HashSet<string> party = playerParties.Values.FirstOrDefault(set => set.Contains(playerName));

        if (party != null)
        {
            RemovePlayerFromParty(ctx, party, playerName);
        }
        else
        {
            LocalizationService.HandleReply(ctx, "You're not in a party.");
        }
    }
}
