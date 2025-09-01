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
    public const string FAMILIAR_INVALID_PREFAB = "Invalid unit prefab (match found but does not start with CHAR/char).";
    public const string FAMILIAR_ADD_SUCCESS = "<color=green>{0}</color> added to <color=white>{1}</color>.";
    public const string FAMILIAR_INVALID_NAME = "Invalid unit name (match found but does not start with CHAR/char).";
    public const string FAMILIAR_ADD_SUCCESS_WITH_HASH = "<color=green>{0}</color> (<color=yellow>{1}</color>) added to <color=white>{2}</color>.";
    public const string FAMILIAR_INVALID_NAME_NO_MATCH = "Invalid unit name (no full or partial matches).";
    public const string FAMILIAR_INVALID_PREFAB_OR_NAME = "Invalid prefab (not an integer) or name (does not start with CHAR/char).";
    public const string FAMILIAR_PRESTIGE_MAX = "Your familiar has already prestiged the maximum number of times! (<color=white>{0}</color>)";
    public const string FAMILIAR_PRESTIGE_RETAIN_LEVEL = "Your familiar has prestiged [<color=#90EE90>{0}</color>]; the accumulated knowledge allowed them to retain their level!";
    public const string FAMILIAR_SCHEMATICS_REMOVE_FAILED = "Failed to remove schematics from your inventory!";

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

    // Generic
    public const string PLAYER_NOT_FOUND = "Couldn't find player...";
    public const string LEVEL_MUST_BE_BETWEEN = "Level must be between 0 and {0}.";

    // Blood Legacy
    public const string BLOOD_NO_PROGRESS = "No progress in <color=red>{0}</color> yet.";
    public const string BLOOD_LOGGING_STATUS = "Blood Legacy logging {0}.";
    public const string BLOOD_STAT_SELECTED = "<color=#00FFFF>{0}</color> selected for <color=red>{1}</color>!";
    public const string BLOOD_LEGACY_SET = "<color=red>{0}</color> legacy set to [<color=white>{1}</color>] for <color=green>{2}</color>";
    public const string BLOOD_AVAILABLE_LEGACIES = "Available Blood Legacies: <color=red>{0}</color>";
    public const string BLOOD_PROGRESS = "You're level [<color=white>{0}</color>][<color=#90EE90>{1}</color>] with <color=yellow>{2}</color> <color=#FFC0CB>essence</color> (<color=white>{3}%</color>) in <color=red>{4}</color>!";
    public const string BLOOD_NO_STATS_SELECTED = "No stats selected for <color=red>{0}</color>, use <color=white>'.bl lst'</color> to see valid options.";
    public const string BLOOD_STATS = "<color=red>{0}</color> Stats: {1}";
    public const string BLOOD_NO_LEGACY_AVAILABLE = "No legacy available for <color=white>{0}</color>.";
    public const string BLOOD_RESET_ITEM_REQUIRED = "You do not have the required item to reset your blood stats (<color=#ffd9eb>{0}</color>x<color=white>{1}</color>)";
    public const string BLOOD_STATS_RESET = "Your blood stats have been reset for <color=red>{0}</color>!";
}
