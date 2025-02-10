using Bloodcraft.Services;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Utilities;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;
using static Bloodcraft.Utilities.Classes;
using static Bloodcraft.Utilities.Misc.PlayerBoolsManager;

namespace Bloodcraft.Commands;

[CommandGroup(name: "class")]
internal static class ClassCommands
{
    static EntityManager EntityManager => Core.EntityManager;

    static readonly bool _classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;

    [Command(name: "choose", shortHand: "c", adminOnly: false, usage: ".class c [Class]", description: "Choose class.")]
    public static void ClassChoiceCommand(ChatCommandContext ctx, string className)
    {
        if (!_classes)
        {
            LocalizationService.HandleReply(ctx, "Classes are not enabled.");
            return;
        }

        if (!TryParseClassName(className, out var parsedClassType))
        {
            LocalizationService.HandleReply(ctx, "Invalid class, use <color=white>'.class l'</color> to see valid options.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;

        if (!HasClass(steamId) && steamId.TryGetPlayerClasses(out var classes)) // retrieval methods here could use improving but this is fine for now
        {
            UpdateClassData(ctx.Event.SenderCharacterEntity, parsedClassType, classes, steamId);
            LocalizationService.HandleReply(ctx, $"You have chosen {FormatClassName(parsedClassType)}!");
        }
        else
        {
            LocalizationService.HandleReply(ctx, $"You have already chosen {FormatClassName(parsedClassType)}, use <color=white>'.class change [Class]'</color> to change. (<color=#ffd9eb>{new PrefabGUID(ConfigService.ChangeClassItem).GetLocalizedName()}</color>x<color=white>{ConfigService.ChangeClassQuantity}</color>)");
        }
    }

    [Command(name: "choosespell", shortHand: "csp", adminOnly: false, usage: ".class csp [#]", description: "Sets shift spell for class if prestige level is high enough.")]
    public static void ChooseClassSpell(ChatCommandContext ctx, int choice)
    {
        if (!_classes)
        {
            LocalizationService.HandleReply(ctx, "Classes are not enabled.");
            return;
        }

        if (!ConfigService.ShiftSlot)
        {
            LocalizationService.HandleReply(ctx, "Shift spells are not enabled.");
            return;
        }

        Entity playerCharacter = ctx.Event.SenderCharacterEntity;

        if (!InventoryUtilities.TryGetInventoryEntity(EntityManager, playerCharacter, out Entity inventoryEntity) || InventoryUtilities.IsInventoryFull(EntityManager, inventoryEntity))
        {
            LocalizationService.HandleReply(ctx, "Can't change or activate class spells when inventory is full, need at least one space to safely handle jewels when switching.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;

        if (HasClass(steamId) && GetPlayerBool(steamId, SHIFT_LOCK_KEY))
        {
            PlayerClass playerClass = GetPlayerClass(steamId);

            if (ConfigService.PrestigeSystem && steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(PrestigeType.Experience, out var prestigeLevel))
            {
                List<int> spells = Configuration.ParseConfigIntegerString(ClassSpellsMap[playerClass]);

                if (spells.Count == 0)
                {
                    LocalizationService.HandleReply(ctx, $"No spells for {FormatClassName(playerClass)} configured!");
                    return;
                }
                else if (choice < 0 || choice > spells.Count)
                {
                    LocalizationService.HandleReply(ctx, $"Invalid spell, use '<color=white>.class lsp</color>' to see options.");
                    return;
                }

                if (choice == 0) // set default for all classes
                {
                    if (ConfigService.DefaultClassSpell == 0)
                    {
                        LocalizationService.HandleReply(ctx, "No spell for class default configured!");
                        return;
                    }
                    else if (prestigeLevel < Configuration.ParseConfigIntegerString(ConfigService.PrestigeLevelsToUnlockClassSpells)[choice])
                    {
                        LocalizationService.HandleReply(ctx, "You don't have the required prestige level for that spell!");
                        return;
                    }
                    else if (steamId.TryGetPlayerSpells(out var data))
                    {
                        PrefabGUID spellPrefabGUID = new(ConfigService.DefaultClassSpell);
                        data.ClassSpell = ConfigService.DefaultClassSpell;

                        steamId.SetPlayerSpells(data);
                        UpdateShift(ctx, playerCharacter, spellPrefabGUID);

                        return;
                    }
                }
                else if (prestigeLevel < Configuration.ParseConfigIntegerString(ConfigService.PrestigeLevelsToUnlockClassSpells)[choice])
                {
                    LocalizationService.HandleReply(ctx, "You don't have the required prestige level for that spell!");
                    return;
                }
                else if (steamId.TryGetPlayerSpells(out var spellsData))
                {
                    spellsData.ClassSpell = spells[choice - 1];
                    steamId.SetPlayerSpells(spellsData);

                    UpdateShift(ctx, ctx.Event.SenderCharacterEntity, new(spellsData.ClassSpell));
                }
            }
            else
            {
                List<int> spells = Configuration.ParseConfigIntegerString(ClassSpellsMap[playerClass]);

                if (spells.Count == 0)
                {
                    LocalizationService.HandleReply(ctx, $"No spells for {FormatClassName(playerClass)} configured!");
                    return;
                }
                else if (choice < 0 || choice > spells.Count)
                {
                    LocalizationService.HandleReply(ctx, $"Invalid spell, use <color=white>'.class lsp'</color> to see valid options.");
                    return;
                }

                if (choice == 0) // set default for all classes
                {
                    if (steamId.TryGetPlayerSpells(out var data))
                    {
                        if (ConfigService.DefaultClassSpell == 0)
                        {
                            LocalizationService.HandleReply(ctx, "No spell for class default configured!");
                            return;
                        }

                        PrefabGUID spellPrefabGUID = new(ConfigService.DefaultClassSpell);
                        data.ClassSpell = ConfigService.DefaultClassSpell;

                        steamId.SetPlayerSpells(data);
                        UpdateShift(ctx, ctx.Event.SenderCharacterEntity, spellPrefabGUID);

                        return;
                    }
                }

                if (steamId.TryGetPlayerSpells(out var spellsData))
                {
                    spellsData.ClassSpell = spells[choice - 1];
                    steamId.SetPlayerSpells(spellsData);

                    UpdateShift(ctx, ctx.Event.SenderCharacterEntity, new(spellsData.ClassSpell));
                }
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, "You haven't selected a class or shift spells aren't enabled! (<color=white>'.class c [Class]'</color> | <color=white>'.shift'</color>)");
        }
    }

    [Command(name: "change", adminOnly: false, usage: ".class change [Class]", description: "Change classes.")]
    public static void ClassChangeCommand(ChatCommandContext ctx, string className)
    {
        if (!_classes)
        {
            LocalizationService.HandleReply(ctx, "Classes are not enabled.");
            return;
        }

        if (!TryParseClassName(className, out var parsedClassType))
        {
            LocalizationService.HandleReply(ctx, "Invalid class, use <color=white>'.class l'</color> to see options.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;
        Entity character = ctx.Event.SenderUserEntity;

        if (steamId.TryGetPlayerClasses(out var classes) && !HasClass(steamId))
        {
            LocalizationService.HandleReply(ctx, "You haven't selected a class to change from yet, use <color=white>'.class c [Class]'</color> instead.");
            return;
        }

        if (ConfigService.ChangeClassItem != 0 && !HandleClassChangeItem(ctx))
        {
            return;
        }

        RemoveClassBuffs(ctx, steamId);
        UpdateClassData(character, parsedClassType, classes, steamId);

        LocalizationService.HandleReply(ctx, $"Class changed to {FormatClassName(parsedClassType)}!");
    }

    [Command(name: "syncbuffs", shortHand: "sb", adminOnly: false, usage: ".class sb", description: "Applies class buffs appropriately if not present.")]
    public static void SyncClassBuffsCommand(ChatCommandContext ctx)
    {
        if (!_classes)
        {
            LocalizationService.HandleReply(ctx, "Classes are not enabled.");
            return;
        }

        var steamId = ctx.Event.User.PlatformId;

        if (HasClass(steamId))
        {
            PlayerClass playerClass = GetPlayerClass(steamId);
            List<int> perks = GetClassBuffs(steamId);

            if (perks.Count == 0)
            {
                LocalizationService.HandleReply(ctx, $"No buffs for {FormatClassName(playerClass)} configured!");
                return;
            }

            Buffs.HandleClassBuffs(ctx.Event.SenderCharacterEntity, steamId);
            LocalizationService.HandleReply(ctx, $"Class buffs applied (if they were missing) for {FormatClassName(playerClass)}!");
        }
        else
        {
            LocalizationService.HandleReply(ctx, "You haven't selected a class!");
        }
    }

    [Command(name: "list", shortHand: "l", adminOnly: false, usage: ".class l", description: "Lists classes.")]
    public static void ListClasses(ChatCommandContext ctx)
    {
        if (!_classes)
        {
            LocalizationService.HandleReply(ctx, "Classes are not enabled.");
            return;
        }

        string classTypes = string.Join(", ", Enum.GetValues(typeof(PlayerClass)).Cast<PlayerClass>().Select(FormatClassName));
        LocalizationService.HandleReply(ctx, $"Classes: {classTypes}");
    }

    [Command(name: "listbuffs", shortHand: "lb", adminOnly: false, usage: ".class lb [Class]", description: "Shows perks that can be gained from class.")]
    public static void ClassPerks(ChatCommandContext ctx, string classType = "")
    {
        if (!_classes)
        {
            LocalizationService.HandleReply(ctx, "Classes are not enabled.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;

        if (HasClass(steamId))
        {
            PlayerClass playerClass = GetPlayerClass(steamId);

            if (!string.IsNullOrEmpty(classType) && TryParseClass(classType, out PlayerClass requestedClass))
            {
                playerClass = requestedClass;
            }

            ReplyClassBuffs(ctx, playerClass);
        }
        else
        {
            if (!string.IsNullOrEmpty(classType) && TryParseClass(classType, out PlayerClass requestedClass))
            {
                ReplyClassBuffs(ctx, requestedClass);
            }
            else
            {
                LocalizationService.HandleReply(ctx, "Invalid class, use <color=white>'.class l'</color> to see options.");
            }
        }
    }

    [Command(name: "listspells", shortHand: "lsp", adminOnly: false, usage: ".class lsp [Class]", description: "Shows spells that can be gained from class.")]
    public static void ListClassSpells(ChatCommandContext ctx, string classType = "")
    {
        if (!_classes)
        {
            LocalizationService.HandleReply(ctx, "Classes are not enabled.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;

        if (HasClass(steamId))
        {
            PlayerClass playerClass = GetPlayerClass(steamId);

            if (!string.IsNullOrEmpty(classType) && TryParseClass(classType, out PlayerClass requestedClass))
            {
                playerClass = requestedClass;
            }

            ReplyClassSpells(ctx, playerClass);
        }
        else
        {
            if (!string.IsNullOrEmpty(classType) && TryParseClass(classType, out PlayerClass requestedClass))
            {
                ReplyClassSpells(ctx, requestedClass);
            }
            else
            {
                LocalizationService.HandleReply(ctx, "Invalid class, use <color=white>'.class l'</color> to see options.");
            }
        }
    }

    [Command(name: "liststats", shortHand: "lst", adminOnly: false, usage: ".class lst [Class]", description: "Shows weapon and blood stat synergies for a class.")]
    public static void ListClassStats(ChatCommandContext ctx, string classType = "")
    {
        if (!_classes)
        {
            LocalizationService.HandleReply(ctx, "Classes are not enabled.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;

        if (HasClass(steamId))
        {
            PlayerClass playerClass = GetPlayerClass(steamId);

            if (!string.IsNullOrEmpty(classType) && TryParseClass(classType, out PlayerClass requestedClass))
            {
                playerClass = requestedClass;
            }

            ReplyClassSynergies(ctx, playerClass);
        }
        else
        {
            if (!string.IsNullOrEmpty(classType) && TryParseClass(classType, out PlayerClass requestedClass))
            {
                ReplyClassSynergies(ctx, requestedClass);
            }
            else
            {
                LocalizationService.HandleReply(ctx, "Invalid class, use <color=white>'.class l'</color> to see options.");
            }
        }
    }
}