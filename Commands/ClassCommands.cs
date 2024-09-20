using Bloodcraft.Services;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Utilities;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;
using static Bloodcraft.Systems.Expertise.WeaponManager.WeaponStats;
using static Bloodcraft.Systems.Legacies.BloodManager.BloodStats;
using static Bloodcraft.Systems.Leveling.LevelingSystem;

namespace Bloodcraft.Commands;

[CommandGroup("class")]
internal static class ClassCommands
{
    static readonly bool Classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;

    [Command(name: "choose", shortHand: "c", adminOnly: false, usage: ".class c [Class]", description: "Choose class.")]
    public static void ClassChoiceCommand(ChatCommandContext ctx, string className)
    {
        if (!Classes)
        {
            LocalizationService.HandleReply(ctx, "Classes are not enabled.");
            return;
        }

        if (!TryParseClassName(className, out var parsedClassType))
        {
            LocalizationService.HandleReply(ctx, "Invalid class, use .classes to see options.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;

        if (!ClassUtilities.HasClass(steamId) && steamId.TryGetPlayerClasses(out var classes)) // retrieval methods here could use improving but this is fine for now
        {
            ClassUtilities.UpdateClassData(ctx.Event.SenderCharacterEntity, parsedClassType, classes, steamId);
            LocalizationService.HandleReply(ctx, $"You have chosen <color=white>{parsedClassType}</color>");
        }
        else
        {
            LocalizationService.HandleReply(ctx, "You have already chosen a class.");
        }
    }

    [Command(name: "choosespell", shortHand: "csp", adminOnly: false, usage: ".class csp [#]", description: "Sets shift spell for class if prestige level is high enough.")]
    public static void ChooseClassSpell(ChatCommandContext ctx, int choice)
    {
        if (!Classes)
        {
            LocalizationService.HandleReply(ctx, "Classes are not enabled.");
            return;
        }

        if (!ConfigService.ShiftSlot)
        {
            LocalizationService.HandleReply(ctx, "Shift slots are not enabled for class spells.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;

        if (ClassUtilities.HasClass(steamId))
        {
            PlayerClasses playerClass = ClassUtilities.GetPlayerClass(steamId);

            if (steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(PrestigeType.Experience, out var prestigeLevel))
            {
                List<int> spells = ConfigUtilities.ParseConfigString(ClassSpellsMap[playerClass]);

                if (spells.Count == 0)
                {
                    LocalizationService.HandleReply(ctx, "No spells found for class.");
                    return;
                }

                if (choice < 0 || choice > spells.Count)
                {
                    LocalizationService.HandleReply(ctx, $"Invalid spell choice. (Use 0-{spells.Count})");
                    return;
                }

                if (choice == 0) // set default for all classes
                {
                    if (steamId.TryGetPlayerSpells(out var data))
                    {
                        data.ClassSpell = ConfigService.DefaultClassSpell;
                        steamId.SetPlayerSpells(data);

                        if (ConfigService.DefaultClassSpell == 0)
                        {
                            LocalizationService.HandleReply(ctx, "No default spell found for classes.");
                            return;
                        }

                        ClassUtilities.UpdateShift(ctx.Event.SenderCharacterEntity, new(data.ClassSpell));
                        LocalizationService.HandleReply(ctx, $"You have chosen <color=#CBC3E3>{new PrefabGUID(ConfigService.DefaultClassSpell).GetPrefabName()}</color>, it will be available on weapons and unarmed if .shift is enabled.");
                        return;
                    }
                }

                if (prestigeLevel < ConfigUtilities.ParseConfigString(ConfigService.PrestigeLevelsToUnlockClassSpells)[choice - 1])
                {
                    LocalizationService.HandleReply(ctx, "You do not have the required prestige level for that spell.");
                    return;
                }

                if (steamId.TryGetPlayerSpells(out var spellsData))
                {
                    spellsData.ClassSpell = spells[choice - 1];
                    steamId.SetPlayerSpells(spellsData);

                    ClassUtilities.UpdateShift(ctx.Event.SenderCharacterEntity, new(spellsData.ClassSpell));
                    LocalizationService.HandleReply(ctx, $"You have chosen <color=#CBC3E3>{new PrefabGUID(spells[choice - 1]).GetPrefabName()}</color> from <color=white>{playerClass}</color>, it will be available on weapons and unarmed if .shift is enabled.");
                }
            }
            else
            {
                LocalizationService.HandleReply(ctx, "You haven't prestiged in leveling yet.");
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, "You haven't chosen a class yet.");
        }
    }

    [Command(name: "change", adminOnly: false, usage: ".class change [Class]", description: "Change classes.")]
    public static void ClassChangeCommand(ChatCommandContext ctx, string className)
    {
        if (!Classes)
        {
            LocalizationService.HandleReply(ctx, "Classes are not enabled.");
            return;
        }

        if (!TryParseClassName(className, out var parsedClassType))
        {
            LocalizationService.HandleReply(ctx, "Invalid class, use .classes to see options.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;
        Entity character = ctx.Event.SenderCharacterEntity;

        if (steamId.TryGetPlayerClasses(out var classes) && !ClassUtilities.HasClass(steamId))
        {
            LocalizationService.HandleReply(ctx, "You haven't chosen a class yet.");
            return;
        }

        if (ConfigService.ChangeClassItem != 0 && !ClassUtilities.HandleClassChangeItem(ctx, steamId))
        {
            return;
        }

        classes.Clear();
        ClassUtilities.RemoveClassBuffs(ctx, steamId);
        ClassUtilities.UpdateClassData(character, parsedClassType, classes, steamId);

        LocalizationService.HandleReply(ctx, $"Class changed to <color=white>{parsedClassType}</color>!");
    }

    [Command(name: "syncbuffs", shortHand: "sb", adminOnly: false, usage: ".class sb", description: "Applies class buffs appropriately if not present.")]
    public static void SyncClassBuffsCommand(ChatCommandContext ctx)
    {
        if (!Classes)
        {
            LocalizationService.HandleReply(ctx, "Classes are not enabled.");
            return;
        }

        var steamId = ctx.Event.User.PlatformId;

        if (ClassUtilities.HasClass(steamId))
        {
            PlayerClasses playerClass = ClassUtilities.GetPlayerClass(steamId);
            List<int> perks = ClassUtilities.GetClassBuffs(steamId);

            if (perks.Count == 0)
            {
                LocalizationService.HandleReply(ctx, "Class buffs not found...");
                return;
            }

            FromCharacter fromCharacter = new()
            {
                Character = ctx.Event.SenderCharacterEntity,
                User = ctx.Event.SenderUserEntity
            };
            BuffUtilities.
                        ApplyClassBuffs(ctx.Event.SenderCharacterEntity, steamId, fromCharacter);
            LocalizationService.HandleReply(ctx, $"Class buffs applied for <color=white>{playerClass}</color>");
        }
        else
        {
            LocalizationService.HandleReply(ctx, "You haven't chosen a class yet.");
        }
    }

    [Command(name: "list", shortHand: "l", adminOnly: false, usage: ".class l", description: "Lists classes.")]
    public static void ListClasses(ChatCommandContext ctx)
    {
        if (!Classes)
        {
            LocalizationService.HandleReply(ctx, "Classes are not enabled.");
            return;
        }

        string classTypes = string.Join(", ", Enum.GetNames(typeof(PlayerClasses)));
        LocalizationService.HandleReply(ctx, $"Available Classes: <color=white>{classTypes}</color>");
    }

    [Command(name: "listbuffs", shortHand: "lb", adminOnly: false, usage: ".class lb [ClassType]", description: "Shows perks that can be gained from class.")]
    public static void ClassPerks(ChatCommandContext ctx, string classType = "")
    {
        if (!Classes)
        {
            LocalizationService.HandleReply(ctx, "Classes are not enabled.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;

        if (ClassUtilities.HasClass(steamId))
        {
            PlayerClasses playerClass = ClassUtilities.GetPlayerClass(steamId);

            if (!string.IsNullOrEmpty(classType) && ClassUtilities.TryParseClass(classType, out PlayerClasses requestedClass))
            {
                playerClass = requestedClass;
            }

            ClassUtilities.
                        ReplyClassBuffs(ctx, playerClass);
        }
        else
        {
            if (!string.IsNullOrEmpty(classType) && ClassUtilities.TryParseClass(classType, out PlayerClasses requestedClass))
            {
                ClassUtilities.ReplyClassBuffs(ctx, requestedClass);
            }
            else
            {
                LocalizationService.HandleReply(ctx, "Invalid class type. Use '.class l' to see options.");
            }
        }
    }

    [Command(name: "listspells", shortHand: "lsp", adminOnly: false, usage: ".class lsp [ClassType]", description: "Shows spells that can be gained from class.")]
    public static void ListClassSpells(ChatCommandContext ctx, string classType = "")
    {
        if (!Classes)
        {
            LocalizationService.HandleReply(ctx, "Classes are not enabled.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;

        if (ClassUtilities.HasClass(steamId))
        {
            PlayerClasses playerClass = ClassUtilities.GetPlayerClass(steamId);

            if (!string.IsNullOrEmpty(classType) && ClassUtilities.TryParseClass(classType, out PlayerClasses requestedClass))
            {
                playerClass = requestedClass;
            }

            ClassUtilities.ReplyClassSpells(ctx, playerClass);
        }
        else
        {
            if (!string.IsNullOrEmpty(classType) && ClassUtilities.TryParseClass(classType, out PlayerClasses requestedClass))
            {
                ClassUtilities.ReplyClassSpells(ctx, requestedClass);
            }
            else
            {
                LocalizationService.HandleReply(ctx, "Invalid class type. Use '.class l' to see options.");
            }
        }
    }

    [Command(name: "liststats", shortHand: "lst", adminOnly: false, usage: ".class lst [Class]", description: "Shows weapon and blood stat synergies for a class.")]
    public static void ListClassStats(ChatCommandContext ctx, string classType = "")
    {
        if (!string.IsNullOrEmpty(classType) && ClassUtilities.TryParseClass(classType, out PlayerClasses requestedClass))
        {
            if (ClassWeaponBloodMap.TryGetValue(requestedClass, out var weaponBloodStats))
            {
                var weaponStats = weaponBloodStats.Item1.Split(',').Select(v => ((WeaponStatType)int.Parse(v)).ToString()).ToList();
                var bloodStats = weaponBloodStats.Item2.Split(',').Select(v => ((BloodStatType)int.Parse(v)).ToString()).ToList();

                if (weaponStats.Count == 0 && bloodStats.Count == 0)
                {
                    LocalizationService.HandleReply(ctx, "No stat synergies found for class.");
                    return;
                }

                var allStats = new List<string>();
                allStats.AddRange(weaponStats.Select(stat => $"<color=white>{stat}</color> (<color=#00FFFF>Weapon</color>)"));
                allStats.AddRange(bloodStats.Select(stat => $"<color=white>{stat}</color> (<color=red>Blood</color>)"));

                for (int i = 0; i < allStats.Count; i += 6)
                {
                    var batch = allStats.Skip(i).Take(6);
                    string replyMessage = string.Join(", ", batch);
                    LocalizationService.HandleReply(ctx, $"{requestedClass} stat synergies[x<color=white>{ConfigService.StatSynergyMultiplier}</color>]: {replyMessage}");
                }
            }
            else
            {
                LocalizationService.HandleReply(ctx, "Stats for the specified class are not configured.");
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Invalid or unspecified class type.");
        }
    }
}