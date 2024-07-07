using Bloodcraft.Services;
using VampireCommandFramework;
using static Bloodcraft.Systems.Experience.PlayerLevelingUtilities.PartyUtilities;

namespace Bloodcraft.Commands
{
    [CommandGroup(name: "party", ".party")]
    internal static class PartyCommands
    {
        static readonly bool PlayerParties = Plugin.Parties.Value;

    [Command(name: "toggleinvites", shortHand: "inv", adminOnly: false, usage: ".party inv", description: "Toggles being able to be invited to parties, prevents damage and share exp.")]
    public static void TogglePartyInvitesCommand(ChatCommandContext ctx)
    {
        if (!PlayerParties)
        {
            LocalizationService.HandleReply(ctx, "Parties are not enabled.");
            return;
        }

        ulong SteamID = ctx.Event.User.PlatformId;
        string name = ctx.Event.User.CharacterName.Value;

        if (Core.DataStructures.PlayerParties.Any(kvp => kvp.Value.Contains(name)))
        {
            LocalizationService.HandleReply(ctx, "You are already in a party. Leave or disband if owned before enabling invites.");
            return;
        }

        if (Core.DataStructures.PlayerBools.TryGetValue(SteamID, out var bools))
        {
            bools["Grouping"] = !bools["Grouping"];
        }
        Core.DataStructures.SavePlayerBools();
        LocalizationService.HandleReply(ctx, $"Party invites {(bools["Grouping"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
    }

    [Command(name: "add", shortHand: "a", adminOnly: false, usage: ".party a [Player]", description: "Adds player to party.")]
    public static void PartyAddCommand(ChatCommandContext ctx, string name)
    {
        if (!PlayerParties)
        {
            LocalizationService.HandleReply(ctx, "Parties are not enabled.");
            return;
        }

        ulong ownerId = ctx.Event.User.PlatformId;
        HandlePlayerParty(ctx, ownerId, name);
    }

    [Command(name: "remove", shortHand: "r", adminOnly: false, usage: ".party r [Player]", description: "Removes player from party.")]
    public static void PartyRemoveCommand(ChatCommandContext ctx, string name)
    {
        if (!PlayerParties)
        {
            LocalizationService.HandleReply(ctx, "Parties are not enabled.");
            return;
        }

        ulong ownerId = ctx.Event.User.PlatformId;

        if (!Core.DataStructures.PlayerParties.ContainsKey(ownerId))
        {
            LocalizationService.HandleReply(ctx, "You don't have a party.");
            return;
        }

        HashSet<string> party = Core.DataStructures.PlayerParties[ownerId]; // check size and if player is already present in group before adding
        RemovePlayerFromParty(ctx, party, name);
    }

    [Command(name: "listmembers", shortHand: "lm", adminOnly: false, usage: ".party lm", description: "Lists party members of your active party.")]
    public static void PartyMembersCommand(ChatCommandContext ctx)
    {
        if (!PlayerParties)
        {
            LocalizationService.HandleReply(ctx, "Parties are not enabled.");
            return;
        }

        Dictionary<ulong, HashSet<string>> playerParties = Core.DataStructures.PlayerParties;

        ListPartyMembers(ctx, playerParties);

    }

    [Command(name: "disband", shortHand: "disband", adminOnly: false, usage: ".party disband", description: "Disbands party.")]
    public static void DisbandPartyCommand(ChatCommandContext ctx)
    {
        if (!PlayerParties)
        {
            LocalizationService.HandleReply(ctx, "Parties are not enabled.");
            return;
        }

        ulong ownerId = ctx.Event.User.PlatformId;

        if (!Core.DataStructures.PlayerParties.ContainsKey(ownerId))
        {
            LocalizationService.HandleReply(ctx, "You don't have a party to disband.");
            return;
        }

        Core.DataStructures.PlayerParties.Remove(ownerId);
        LocalizationService.HandleReply(ctx, "Party disbanded.");
        Core.DataStructures.SavePlayerParties();
    }

    [Command(name: "leave", shortHand: "l", adminOnly: false, usage: ".party l", description: "Leaves party if in one.")]
    public static void LeavePartyCommand(ChatCommandContext ctx)
    {
        if (!PlayerParties)
        {
            LocalizationService.HandleReply(ctx, "Parties are not enabled.");
            return;
        }

        ulong ownerId = ctx.Event.User.PlatformId;
        string playerName = ctx.Event.User.CharacterName.Value;

        if (Core.DataStructures.PlayerParties.ContainsKey(ownerId))
        {
            LocalizationService.HandleReply(ctx, "You can't leave your own party. Disband it instead.");
            return;
        }

        var party = Core.DataStructures.PlayerParties.Values.FirstOrDefault(set => set.Contains(playerName));
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
