using Bloodcraft.Services;
using Bloodcraft.Systems.Experience;
using Bloodcraft.Systems.Leveling;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;
using static Bloodcraft.Systems.Experience.PlayerLevelingUtilities;
using static Bloodcraft.Systems.Expertise.ExpertiseStats.WeaponStatManager;
using static Bloodcraft.Systems.Legacies.LegacyStats.BloodStatManager;

namespace Bloodcraft.Commands;

[CommandGroup("class")]
internal static class ClassCommands
{
    static readonly bool SoftSynergies = Plugin.SoftSynergies.Value;
    static readonly bool HardSynergies = Plugin.HardSynergies.Value;
    static readonly bool ShiftSlot = Plugin.ShiftSlot.Value;

    [Command(name: "choose", shortHand: "c", adminOnly: false, usage: ".class c [Class]", description: "Choose class.")]
    public static void ClassChoiceCommand(ChatCommandContext ctx, string className)
    {
        if (!SoftSynergies && !HardSynergies)
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

        if (Core.DataStructures.PlayerClasses.TryGetValue(steamId, out var classes))
        {
            if (classes.Keys.Count > 0)
            {
                LocalizationService.HandleReply(ctx, "You have already chosen a class.");
                return;
            }
            UpdateClassData(ctx.Event.SenderCharacterEntity, parsedClassType, classes, steamId);
            LocalizationService.HandleReply(ctx, $"You have chosen <color=white>{parsedClassType}</color>");
        }
    }

    [Command(name: "choosespell", shortHand: "csp", adminOnly: false, usage: ".class csp [#]", description: "Sets shift spell for class if prestige level is high enough.")]
    public static void ChooseClassSpell(ChatCommandContext ctx, int choice)
    {
        if (!SoftSynergies && !HardSynergies)
        {
            LocalizationService.HandleReply(ctx, "Classes are not enabled.");
            return;
        }
        if (!ShiftSlot)
        {
            LocalizationService.HandleReply(ctx, "Shift slots are not enabled for class spells.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;

        if (Core.DataStructures.PlayerClasses.TryGetValue(steamId, out var classes))
        {
            if (classes.Keys.Count == 0)
            {
                LocalizationService.HandleReply(ctx, "You haven't chosen a class yet.");
                return;
            }
            PlayerClasses playerClass = classes.Keys.FirstOrDefault();
            if (Core.DataStructures.PlayerPrestiges.TryGetValue(steamId, out var prestigeData) && prestigeData.TryGetValue(PrestigeUtilities.PrestigeType.Experience, out var prestigeLevel))
            {
                if (prestigeLevel < Core.ParseConfigString(Plugin.PrestigeLevelsToUnlockClassSpells.Value)[choice - 1])
                {
                    LocalizationService.HandleReply(ctx, "You do not have the required prestige level for that spell.");
                    return;
                }

                List<int> spells = Core.ParseConfigString(PlayerLevelingUtilities.ClassSpellsMap[playerClass]);

                if (spells.Count == 0)
                {
                    LocalizationService.HandleReply(ctx, "No spells found for class.");
                    return;
                }

                if (choice < 1 || choice > spells.Count)
                {
                    LocalizationService.HandleReply(ctx, $"Invalid spell choice. (Use 1-{spells.Count})");
                    return;
                }

                if (Core.DataStructures.PlayerSpells.TryGetValue(steamId, out var spellsData))
                {
                    spellsData.ClassSpell = spells[choice - 1];
                    Core.DataStructures.PlayerSpells[steamId] = spellsData;
                    Core.DataStructures.SavePlayerSpells();

                    LocalizationService.HandleReply(ctx, $"You have chosen spell <color=#CBC3E3>{new PrefabGUID(spells[choice - 1]).LookupName()}</color> from <color=white>{playerClass}</color>, it will be available on weapons and unarmed if .shift is enabled.");
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
        if (!SoftSynergies && !HardSynergies)
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

        if (!Core.DataStructures.PlayerClasses.TryGetValue(steamId, out var classes))
        {
            LocalizationService.HandleReply(ctx, "You haven't chosen a class yet.");
            return;
        }

        if (Plugin.ChangeClassItem.Value != 0 && !HandleClassChangeItem(ctx, steamId))
        {
            LocalizationService.HandleReply(ctx, $"You do not have the required item to change classes. ({new PrefabGUID(Plugin.ChangeClassItem.Value).GetPrefabName()}x{Plugin.ChangeClassItemQuantity.Value})");
            return;
        }

        RemoveClassBuffs(ctx, steamId);

        classes.Clear();
        UpdateClassData(character, parsedClassType, classes, steamId);
        LocalizationService.HandleReply(ctx, $"You have changed to <color=white>{parsedClassType}</color>");
    }

    [Command(name: "syncbuffs", shortHand: "sb", adminOnly: false, usage: ".class sb", description: "Applies class buffs appropriately if not present.")]
    public static void SyncClassBuffsCommand(ChatCommandContext ctx)
    {
        if (!SoftSynergies && !HardSynergies)
        {
            LocalizationService.HandleReply(ctx, "Classes are not enabled.");
            return;
        }

        var steamId = ctx.Event.User.PlatformId;

        if (Core.DataStructures.PlayerClasses.TryGetValue(steamId, out var classes))
        {
            if (classes.Keys.Count == 0)
            {
                LocalizationService.HandleReply(ctx, "You haven't chosen a class yet.");
                return;
            }
            PlayerClasses playerClass = classes.Keys.FirstOrDefault();
            List<int> perks = PlayerLevelingUtilities.GetClassBuffs(steamId);

            if (perks.Count == 0)
            {
                LocalizationService.HandleReply(ctx, "Class buffs not found.");
                return;
            }

            FromCharacter fromCharacter = new()
            {
                Character = ctx.Event.SenderCharacterEntity,
                User = ctx.Event.SenderUserEntity
            };

            ApplyClassBuffs(ctx.Event.SenderCharacterEntity, steamId, Core.DebugEventsSystem, fromCharacter);
            LocalizationService.HandleReply(ctx, $"Class buffs applied for <color=white>{playerClass}</color>");
        }
    }

    [Command(name: "list", shortHand: "l", adminOnly: false, usage: ".class l", description: "Lists classes.")]
    public static void ListClasses(ChatCommandContext ctx)
    {
        if (!SoftSynergies && !HardSynergies)
        {
            LocalizationService.HandleReply(ctx, "Classes are not enabled.");
            return;
        }

        string classTypes = string.Join(", ", Enum.GetNames(typeof(PlayerLevelingUtilities.PlayerClasses)));
        LocalizationService.HandleReply(ctx, $"Available Classes: <color=white>{classTypes}</color>");
    }
    /*
    [Command(name: "listbuffs", shortHand: "lb", adminOnly: false, usage: ".class lb [ClassType]", description: "Shows perks that can be gained from class.")]
    public static void ClassPerks(ChatCommandContext ctx, string classType = "")
    {
        if (!SoftSynergies && !HardSynergies)
        {
            LocalizationService.HandleReply(ctx, "Classes are not enabled.");
            return;
        }
        ulong steamId = ctx.Event.User.PlatformId;
        if (Core.DataStructures.PlayerClasses.TryGetValue(steamId, out var classes))
        {
            if (classes.Keys.Count == 0)
            {
                LocalizationService.HandleReply(ctx, "You haven't chosen a class yet.");
                return;
            }

            // Parse classType parameter
            PlayerClasses playerClass;
            if (!string.IsNullOrEmpty(classType) && TryParseClass(classType, out PlayerClasses requestedClass))
            {
                playerClass = requestedClass;
            }
            else
            {
                playerClass = classes.Keys.FirstOrDefault();
            }

            List<int> perks = Core.ParseConfigString(PlayerLevelingUtilities.ClassPrestigeBuffsMap[playerClass]);

            if (perks.Count == 0)
            {
                LocalizationService.HandleReply(ctx, "Class buffs not found.");
                return;
            }

            int step = MaxPlayerLevel / perks.Count;

            var classBuffs = perks.Select((perk, index) =>
            {
                int level = (index + 1) * step;
                string prefab = new PrefabGUID(perk).LookupName();
                int prefabIndex = prefab.IndexOf("Prefab");
                if (prefabIndex != -1)
                {
                    prefab = prefab[..prefabIndex].TrimEnd();
                }
                return $"<color=white>{prefab}</color> at level <color=yellow>{level}</color>";
            }).ToList();

            for (int i = 0; i < classBuffs.Count; i += 6)
            {
                var batch = classBuffs.Skip(i).Take(6);
                string replyMessage = string.Join(", ", batch);
                LocalizationService.HandleReply(ctx, $"{playerClass} buffs: {replyMessage}");
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, "You haven't chosen a class yet.");
        }
    }

    [Command(name: "listspells", shortHand: "lsp", adminOnly: false, usage: ".class lsp [ClassType]", description: "Shows spells that can be gained from class.")]
    public static void ListClassSpells(ChatCommandContext ctx, string classType = "")
    {
        if (!SoftSynergies && !HardSynergies)
        {
            LocalizationService.HandleReply(ctx, "Classes are not enabled.");
            return;
        }
        ulong steamId = ctx.Event.User.PlatformId;
        if (Core.DataStructures.PlayerClasses.TryGetValue(steamId, out var classes))
        {
            if (classes.Keys.Count == 0)
            {
                LocalizationService.HandleReply(ctx, "You haven't chosen a class yet.");
                return;
            }

            // Parse classType parameter
            PlayerClasses playerClass;
            if (!string.IsNullOrEmpty(classType) && TryParseClass(classType, out PlayerClasses requestedClass))
            {
                playerClass = requestedClass;
            }
            else
            {
                playerClass = classes.Keys.FirstOrDefault();
            }

            List<int> perks = Core.ParseConfigString(PlayerLevelingUtilities.ClassSpellsMap[playerClass]);

            if (perks.Count == 0)
            {
                LocalizationService.HandleReply(ctx, "Class spells not found.");
                return;
            }

            var classSpells = perks.Select(perk =>
            {
                string prefab = new PrefabGUID(perk).LookupName();
                int prefabIndex = prefab.IndexOf("Prefab");
                if (prefabIndex != -1)
                {
                    prefab = prefab[..prefabIndex].TrimEnd();
                }
                return $"<color=white>{prefab}</color>";
            }).ToList();

            for (int i = 0; i < classSpells.Count; i += 6)
            {
                var batch = classSpells.Skip(i).Take(6);
                string replyMessage = string.Join(", ", batch);
                LocalizationService.HandleReply(ctx, $"{playerClass} spells: {replyMessage}");
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, "You haven't chosen a class yet.");
        }
    }
    */
    [Command(name: "listbuffs", shortHand: "lb", adminOnly: false, usage: ".class lb [ClassType]", description: "Shows perks that can be gained from class.")]
    public static void ClassPerks(ChatCommandContext ctx, string classType = "")
    {
        if (!SoftSynergies && !HardSynergies)
        {
            LocalizationService.HandleReply(ctx, "Classes are not enabled.");
            return;
        }
        ulong steamId = ctx.Event.User.PlatformId;
        if (Core.DataStructures.PlayerClasses.TryGetValue(steamId, out var classes))
        {
            // Parse classType parameter
            PlayerClasses playerClass;
            if (!string.IsNullOrEmpty(classType) && TryParseClass(classType, out PlayerClasses requestedClass))
            {
                playerClass = requestedClass;
            }
            else
            {
                playerClass = classes.Keys.FirstOrDefault();
            }

            ShowClassBuffs(ctx, playerClass);
        }
        else
        {
            if (string.IsNullOrEmpty(classType))
            {
                foreach (PlayerClasses cls in Enum.GetValues(typeof(PlayerClasses)))
                {
                    ShowClassBuffs(ctx, cls);
                }
            }
            else if (TryParseClass(classType, out PlayerClasses requestedClass))
            {
                ShowClassBuffs(ctx, requestedClass);
            }
            else
            {
                LocalizationService.HandleReply(ctx, "Invalid class type.");
            }
        }
    }
    [Command(name: "listspells", shortHand: "lsp", adminOnly: false, usage: ".class lsp [ClassType]", description: "Shows spells that can be gained from class.")]
    public static void ListClassSpells(ChatCommandContext ctx, string classType = "")
    {
        if (!SoftSynergies && !HardSynergies)
        {
            LocalizationService.HandleReply(ctx, "Classes are not enabled.");
            return;
        }
        ulong steamId = ctx.Event.User.PlatformId;
        if (Core.DataStructures.PlayerClasses.TryGetValue(steamId, out var classes))
        {
            // Parse classType parameter
            PlayerClasses playerClass;
            if (!string.IsNullOrEmpty(classType) && TryParseClass(classType, out PlayerClasses requestedClass))
            {
                playerClass = requestedClass;
            }
            else
            {
                playerClass = classes.Keys.FirstOrDefault();
            }

            ShowClassSpells(ctx, playerClass);
        }
        else
        {
            if (string.IsNullOrEmpty(classType))
            {
                foreach (PlayerClasses cls in Enum.GetValues(typeof(PlayerClasses)))
                {
                    ShowClassSpells(ctx, cls);
                }
            }
            else if (TryParseClass(classType, out PlayerClasses requestedClass))
            {
                ShowClassSpells(ctx, requestedClass);
            }
            else
            {
                LocalizationService.HandleReply(ctx, "Invalid class type.");
            }
        }
    }
    [Command(name: "liststats", shortHand: "lst", adminOnly: false, usage: ".class lst [Class]", description: "Shows weapon and blood stat synergies for a class.")]
    public static void ListClassStats(ChatCommandContext ctx, string classType = "")
    {
        // Parse classType parameter
        if (!string.IsNullOrEmpty(classType) && TryParseClass(classType, out PlayerClasses requestedClass))
        {
            if (ClassWeaponBloodMap.TryGetValue(requestedClass, out var weaponBloodStats))
            {
                var weaponStats = weaponBloodStats.Item1.Split(',').Select(v => ((WeaponStatType)int.Parse(v)).ToString()).ToList();
                var bloodStats = weaponBloodStats.Item2.Split(',').Select(v => ((BloodStatType)int.Parse(v)).ToString()).ToList();

                if (weaponStats.Count == 0 && bloodStats.Count == 0)
                {
                    LocalizationService.HandleReply(ctx, "No stats found for the specified class.");
                    return;
                }

                var allStats = new List<string>();
                allStats.AddRange(weaponStats.Select(stat => $"<color=white>{stat}</color> (<color=#00FFFF>Weapon</color>)"));
                allStats.AddRange(bloodStats.Select(stat => $"<color=white>{stat}</color> (<color=red>Blood</color>)"));

                for (int i = 0; i < allStats.Count; i += 6)
                {
                    var batch = allStats.Skip(i).Take(6);
                    string replyMessage = string.Join(", ", batch);
                    LocalizationService.HandleReply(ctx, $"{requestedClass} stat synergies[x<color=white>{Plugin.StatSynergyMultiplier.Value}</color>]: {replyMessage}");
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