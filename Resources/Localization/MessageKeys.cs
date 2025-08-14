namespace Bloodcraft.Resources.Localization;

internal static class MessageKeys
{
    public const string EMOTE_ACTIONS_DOMINATE_FORM = "You can't use emote actions when using dominate form!";
    public const string EMOTE_ACTIONS_BAT_FORM = "You can't use emote actions when using bat form!";
    public const string FAMILIAR_INTERACT_COMBAT = "You can't interact with your familiar during combat!";
    public const string SHAPESHIFT_SELECT_FORM = "Select a form you've unlocked first! ('<color=white>.prestige sf [<color=orange>EvolvedVampire|CorruptedSerpent</color>]</color>')";
    public const string SHAPESHIFT_NOT_ENOUGH_ENERGY = "Not enough energy to maintain form... (<color=yellow>{0}</color>)";
    public const string FAMILIAR_CALL_PVP_COMBAT = "You can't call your familiar during PvP combat!";
    public const string FAMILIAR_ACTIVE_NOT_EXIST = "Active familiar doesn't exist! If that doesn't seem right try using '<color=white>.fam reset</color>'.";
    public const string FAMILIAR_NOT_FOUND = "Couldn't find active familiar...";
    public const string FAMILIAR_COMBAT_NOT_ENABLED = "Familiar combat is not enabled.";
    public const string FAMILIAR_COMBAT_TOGGLE_IN_COMBAT = "You can't toggle familiar combat mode during PvE/PvP combat!";
    public const string FAMILIAR_COMBAT_ENABLED = "Familiar combat <color=green>enabled</color>.";
    public const string FAMILIAR_COMBAT_DISABLED = "Familiar combat <color=red>disabled</color>.";
    public const string FAMILIAR_INTERACT_DISMISSED = "Can't interact with familiar when dismissed!";
    public const string FAMILIAR_INTERACT_BINDING = "Can't interact with familiar when binding/unbinding!";

    public const string CLASS_CHANGE_MISSING_ITEM = "You do not have enough of the required item to change classes (<color=#ffd9eb>{0}</color>x<color=white>{1}</color>)";
    public const string CLASS_CHANGE_REMOVE_FAILED = "Failed to remove enough of the item required (<color=#ffd9eb>{0}</color>x<color=white>{1}</color>)";
    public const string CLASS_PASSIVES_NOT_FOUND = "{0} passives not found!";
    public const string CLASS_PASSIVE_ENTRY = "<color=yellow>{0}</color>| {1} at level <color=green>{2}</color>";
    public const string CLASS_PASSIVES_HEADER = "{0} passives:";
    public const string CLASS_SYNERGIES_NOT_FOUND = "Couldn't find stat synergies for {0}...";
    public const string CLASS_SYNERGY_WEAPON_ENTRY = "<color=white>{0}</color> (<color=#00FFFF>Weapon</color>)";
    public const string CLASS_SYNERGY_BLOOD_ENTRY = "<color=white>{0}</color> (<color=red>Blood</color>)";
    public const string CLASS_SYNERGIES_NONE = "No stat synergies found for {0}.";
    public const string CLASS_SYNERGIES_HEADER = "{0} stat synergies [x<color=white>{1}</color>]:";
    public const string CLASS_SPELLS_NONE = "{0} has no spells configured...";
    public const string CLASS_SPELLS_HEADER = "{0} spells:";
    public const string CLASS_SHIFT_SPELL = "Shift spell: <color=#CBC3E3>{0}</color>";
    public const string CLASS_INVALID = "Invalid class, use <color=white>'.class l'</color> to see options.";
    public const string CLASS_BUFFS_REFRESHED = "Removed all class buffs then applied current class buffs for all players.";
    public const string CLASS_BUFFS_REMOVED = "Removed all class buffs for all players.";
}
