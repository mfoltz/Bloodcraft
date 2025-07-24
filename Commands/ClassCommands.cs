using Bloodcraft.Interfaces;
using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;
using static Bloodcraft.Systems.Leveling.ClassManager;
using static Bloodcraft.Utilities.Classes;
using static Bloodcraft.Utilities.Misc.PlayerBools;
using static VCF.Core.Basics.RoleCommands;
using User = ProjectM.Network.User;

namespace Bloodcraft.Commands;

[CommandGroup(name: "class")]
internal static class ClassCommands
{
    static EntityManager EntityManager => Core.EntityManager;

    static readonly bool _classes = ConfigService.ClassSystem;

    [Command(name: "select", shortHand: "s", adminOnly: false, usage: ".class s [Class]", description: "Select class.")]
    public static void SelectClassCommand(ChatCommandContext ctx, string input)
    {
        if (!_classes)
        {
            LocalizationService.HandleReply(ctx, "Classes are not enabled.");
            return;
        }

        Entity playerCharacter = ctx.Event.SenderCharacterEntity;
        ulong steamId = ctx.Event.User.PlatformId;
        PlayerClass? nullablePlayerClass = ParseClassFromInput(ctx, input);

        if (nullablePlayerClass.HasValue)
        {
            PlayerClass playerClass = nullablePlayerClass.Value;

            if (!steamId.HasClass(out PlayerClass? currentClass) || !currentClass.HasValue)
            {
                UpdatePlayerClass(playerCharacter, playerClass, steamId);
                // ApplyClassBuffs(playerCharacter, steamId);

                LocalizationService.HandleReply(ctx, $"You've selected {FormatClassName(playerClass)}!");
            }
            else
            {
                LocalizationService.HandleReply(ctx, $"You've already selected {FormatClassName(currentClass.Value)}, use <color=white>'.class c [Class]'</color> to change. (<color=#ffd9eb>{new PrefabGUID(ConfigService.ChangeClassItem).GetLocalizedName()}</color>x<color=white>{ConfigService.ChangeClassQuantity}</color>)");
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Invalid class, use '<color=white>.class l</color>' to see options.");
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

        if (GetPlayerBool(steamId, SHIFT_LOCK_KEY) 
            && steamId.HasClass(out PlayerClass? playerClass) 
            && playerClass.HasValue)
        {
            if (ConfigService.PrestigeSystem && steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(PrestigeType.Experience, out var prestigeLevel))
            {
                List<int> spells = Configuration.ParseIntegersFromString(ClassSpellsMap[playerClass.Value]);

                if (spells.Count == 0)
                {
                    LocalizationService.HandleReply(ctx, $"No spells for {FormatClassName(playerClass.Value)} configured!");
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
                    else if (prestigeLevel < Configuration.ParseIntegersFromString(ConfigService.PrestigeLevelsToUnlockClassSpells)[choice])
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
                else if (prestigeLevel < Configuration.ParseIntegersFromString(ConfigService.PrestigeLevelsToUnlockClassSpells)[choice])
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
                List<int> spells = Configuration.ParseIntegersFromString(ClassSpellsMap[playerClass.Value]);

                if (spells.Count == 0)
                {
                    LocalizationService.HandleReply(ctx, $"No spells for {FormatClassName(playerClass.Value)} configured!");
                    return;
                }
                else if (choice < 0 || choice > spells.Count)
                {
                    LocalizationService.HandleReply(ctx, $"Invalid spell, use <color=white>'.class lsp'</color> to see options.");
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
            LocalizationService.HandleReply(ctx, "You haven't selected a class or you haven't activated shift spells! (<color=white>'.class s [Class]'</color> | <color=white>'.class shift'</color>)");
        }
    }

    [Command(name: "change", shortHand: "c", adminOnly: false, usage: ".class c [Class]", description: "Change classes.")]
    public static void ChangeClassCommand(ChatCommandContext ctx, string input)
    {
        if (!_classes)
        {
            LocalizationService.HandleReply(ctx, "Classes are not enabled.");
            return;
        }

        Entity playerCharacter = ctx.Event.SenderUserEntity;
        ulong steamId = ctx.Event.User.PlatformId;

        PlayerClass? nullablePlayerClass = ParseClassFromInput(ctx, input);

        if (nullablePlayerClass.HasValue)
        {
            PlayerClass playerClass = nullablePlayerClass.Value;

            if (!steamId.HasClass(out PlayerClass? currentClass) || !currentClass.HasValue)
            {
                LocalizationService.HandleReply(ctx, "You haven't selected a class yet, use <color=white>'.class s [Class]'</color> instead.");
                return;
            }

            if (GetPlayerBool(steamId, CLASS_BUFFS_KEY))
            {
                LocalizationService.HandleReply(ctx, "You have class buffs enabled, use <color=white>'.class passives'</color> to disable them before changing classes!");
                return;
            }

            if (ConfigService.ChangeClassItem != 0 && !HandleClassChangeItem(ctx))
            {
                return;
            }

            UpdatePlayerClass(playerCharacter, playerClass, steamId);
            LocalizationService.HandleReply(ctx, $"Class changed to {FormatClassName(playerClass)}!");
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Invalid class, use '<color=white>.class l</color>' to see options.");
        }
    }

    [Command(name: "list", shortHand: "l", adminOnly: false, usage: ".class l", description: "List available classes.")]
    public static void ListClasses(ChatCommandContext ctx)
    {
        if (!_classes)
        {
            LocalizationService.HandleReply(ctx, "Classes are not enabled.");
            return;
        }

        var classes = Enum.GetValues(typeof(PlayerClass)).Cast<PlayerClass>().Select((playerClass, index) =>
        {
            return $"<color=yellow>{index + 1}</color>| {FormatClassName(playerClass, false)}";
        }).ToList();

        string classTypes = string.Join(", ", classes);
        LocalizationService.HandleReply(ctx, $"Classes: {classTypes}");
    }

    [Command(name: "listspells", shortHand: "lsp", adminOnly: false, usage: ".class lsp [Class]", description: "Shows spells that can be gained from class.")]
    public static void ListClassSpellsCommand(ChatCommandContext ctx, string classType = "")
    {
        if (!_classes)
        {
            LocalizationService.HandleReply(ctx, "Classes are not enabled.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;
        PlayerClass? nullablePlayerClass = ParseClassFromInput(ctx, classType);

        if (nullablePlayerClass.HasValue)
        {
            ReplyClassSpells(ctx, nullablePlayerClass.Value);
        }
        else if (string.IsNullOrEmpty(classType) && steamId.HasClass(out PlayerClass? currentClass) && currentClass.HasValue)
        {
            ReplyClassSpells(ctx, currentClass.Value);
        }

        /*
        else
        {
            LocalizationService.HandleReply(ctx, "Invalid class, use '<color=white>.class l</color>' to see options.");
        }

        ulong steamId = ctx.Event.User.PlatformId;

        if (steamId.HasClass(out PlayerClass? playerClass)
            && playerClass.HasValue)
        {
            if (!string.IsNullOrEmpty(classType) && TryParseClass(classType, out PlayerClass requestedClass))
            {
                playerClass = requestedClass;
            }

            ReplyClassSpells(ctx, playerClass.Value);
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
        */
    }

    [Command(name: "liststats", shortHand: "lst", adminOnly: false, usage: ".class lst [Class]", description: "List weapon and blood stat synergies for a class.")]
    public static void ListClassStatsCommand(ChatCommandContext ctx, string classType = "")
    {
        if (!_classes)
        {
            LocalizationService.HandleReply(ctx, "Classes are not enabled.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;
        PlayerClass? nullablePlayerClass = ParseClassFromInput(ctx, classType);

        if (nullablePlayerClass.HasValue)
        {
            ReplyClassSynergies(ctx, nullablePlayerClass.Value);
        }
        else if (string.IsNullOrEmpty(classType) && steamId.HasClass(out PlayerClass? currentClass) && currentClass.HasValue)
        {
            ReplyClassSynergies(ctx, currentClass.Value);
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Invalid class, use '<color=white>.class l</color>' to see options.");
        }

        /*
        ulong steamId = ctx.Event.User.PlatformId;

        if (steamId.HasClass(out PlayerClass? playerClass)
            && playerClass.HasValue)
        {
            if (!string.IsNullOrEmpty(classType) && TryParseClass(classType, out PlayerClass requestedClass))
            {
                playerClass = requestedClass;
            }

            ReplyClassSynergies(ctx, playerClass.Value);
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
        */
    }

    [Command(name: "lockshift", shortHand: "shift", adminOnly: false, usage: ".class shift", description: "Toggle shift spell.")]
    public static void ShiftSlotToggleCommand(ChatCommandContext ctx)
    {
        if (!_classes)
        {
            LocalizationService.HandleReply(ctx, "Classes are not enabled and spells can't be set to shift.");
            return;
        }

        if (!ConfigService.ShiftSlot)
        {
            LocalizationService.HandleReply(ctx, "Shift slots are not enabled.");
            return;
        }

        Entity character = ctx.Event.SenderCharacterEntity;
        User user = ctx.Event.User;

        ulong steamId = user.PlatformId;

        if (!InventoryUtilities.TryGetInventoryEntity(EntityManager, character, out Entity inventoryEntity) || InventoryUtilities.IsInventoryFull(EntityManager, inventoryEntity))
        {
            LocalizationService.HandleReply(ctx, "Can't change or active class spells when inventory is full, need at least one space to safely handle jewels when switching.");
            return;
        }

        TogglePlayerBool(steamId, SHIFT_LOCK_KEY);
        if (GetPlayerBool(steamId, SHIFT_LOCK_KEY))
        {
            if (steamId.TryGetPlayerSpells(out var spellsData))
            {
                PrefabGUID spellPrefabGUID = new(spellsData.ClassSpell);

                if (spellPrefabGUID.HasValue())
                {
                    UpdateShift(ctx, ctx.Event.SenderCharacterEntity, spellPrefabGUID);
                }
            }

            LocalizationService.HandleReply(ctx, "Shift spell <color=green>enabled</color>!");
        }
        else
        {
            RemoveShift(ctx.Event.SenderCharacterEntity);

            LocalizationService.HandleReply(ctx, "Shift spell <color=red>disabled</color>!");
        }
    }
}