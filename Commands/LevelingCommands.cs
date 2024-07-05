using Bloodcraft.Patches;
using Bloodcraft.Services;
using Bloodcraft.Systems.Experience;
using ProjectM.Network;
using Unity.Entities;
using VampireCommandFramework;
using static Bloodcraft.Systems.Experience.PlayerLevelingUtilities;
using static Bloodcraft.Systems.Experience.PlayerLevelingUtilities.PartyUtilities;
using static Bloodcraft.Systems.Expertise.ExpertiseStats.WeaponStatManager;
using static Bloodcraft.Systems.Legacies.LegacyStats.BloodStatManager;

namespace Bloodcraft.Commands
{
    [CommandGroup(name:"level", ".lvl")]
internal static class LevelingCommands
{
    //static VampireStatModifiers VampireStatModifiers => Core.ServerGameSettingsSystem._Settings.VampireStatModifiers;
    static readonly bool Leveling = Plugin.LevelingSystem.Value;
    static readonly bool SoftSynergies = Plugin.SoftSynergies.Value;
    static readonly bool HardSynergies = Plugin.HardSynergies.Value;
    static readonly bool ShiftSlot = Plugin.ShiftSlot.Value;
    static readonly bool PlayerParties = Plugin.Parties.Value;
    static readonly bool Prestige = Plugin.PrestigeSystem.Value;
    static readonly int MaxPlayerLevel = Plugin.MaxPlayerLevel.Value;

 
    [Command(name: "log", shortHand: "log", adminOnly: false, usage: ".lvl log", description: "Toggles leveling progress logging.")]
    public static void LogExperienceCommand(ChatCommandContext ctx)
    {
        if (!Leveling)
        {
            LocalizationService.HandleReply(ctx, "Leveling is not enabled.");
            return;
        }
        var SteamID = ctx.Event.User.PlatformId;

        if (Core.DataStructures.PlayerBools.TryGetValue(SteamID, out var bools))
        {
            bools["ExperienceLogging"] = !bools["ExperienceLogging"];
        }
        Core.DataStructures.SavePlayerBools();
        LocalizationService.HandleReply(ctx, $"Leveling experience logging {(bools["ExperienceLogging"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
    }

    [Command(name: "get", shortHand: "get", adminOnly: false, usage: ".lvl get", description: "Display current leveling progress.")]
    public static void GetLevelCommand(ChatCommandContext ctx)
    {
        if (!Leveling)
        {
            LocalizationService.HandleReply(ctx, "Leveling is not enabled.");
            return;
        }
        ulong steamId = ctx.Event.User.PlatformId;
        if (Core.DataStructures.PlayerExperience.TryGetValue(steamId, out var levelKvp))
        {
            int level = levelKvp.Key;
            int progress = (int)(levelKvp.Value - PlayerLevelingUtilities.ConvertLevelToXp(level));
            int percent = PlayerLevelingUtilities.GetLevelProgress(steamId);
            LocalizationService.HandleReply(ctx, $"You're level [<color=white>{level}</color>] and have <color=yellow>{progress}</color> <color=#FFC0CB>experience</color> (<color=white>{percent}%</color>)");
        }
        else
        {
            LocalizationService.HandleReply(ctx, "No experience yet.");
        }
    }

    [Command(name: "set", shortHand: "set", adminOnly: true, usage: ".lvl set [Player] [Level]", description: "Sets player level.")]
    public static void SetLevelCommand(ChatCommandContext ctx, string name, int level)
    {
        if (!Leveling)
        {
            LocalizationService.HandleReply(ctx, "Leveling is not enabled.");
            return;
        }

        Entity foundUserEntity = PlayerService.GetUserByName(name, true);
        if (foundUserEntity.Equals(Entity.Null))
        {
            LocalizationService.HandleReply(ctx, "Player not found...");
            return;
        }
        User foundUser = foundUserEntity.Read<User>();

        if (level < 0 || level > MaxPlayerLevel)
        {
            LocalizationService.HandleReply(ctx, $"Level must be between 0 and {MaxPlayerLevel}");
            return;
        }
        ulong steamId = foundUser.PlatformId;
        if (Core.DataStructures.PlayerExperience.TryGetValue(steamId, out var _))
        {
            var xpData = new KeyValuePair<int, float>(level, PlayerLevelingUtilities.ConvertLevelToXp(level));
            Core.DataStructures.PlayerExperience[steamId] = xpData;
            Core.DataStructures.SavePlayerExperience();
            GearOverride.SetLevel(foundUser.LocalCharacter._Entity);
            LocalizationService.HandleReply(ctx, $"Level set to <color=white>{level}</color> for <color=green>{foundUser.CharacterName}</color>");
        }
        else
        {
            LocalizationService.HandleReply(ctx, "No experience found.");
        }
    }

    [Command(name: "chooseClass", shortHand: "cc", adminOnly: false, usage: ".cc [Class]", description: "Choose class.")]
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

    [Command(name: "chooseClassSpell", shortHand: "cs", adminOnly: false, usage: ".cs [#]", description: "Sets shift spell for class if prestige level is high enough.")]
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
            if (Core.DataStructures.PlayerPrestiges.TryGetValue(steamId, out var prestigeData) && prestigeData.TryGetValue(PrestigeSystem.PrestigeType.Experience, out var prestigeLevel))
            {
                if (prestigeLevel < Core.ParseConfigString(Plugin.PrestigeLevelsToUnlockClassSpells.Value)[choice - 1])
                {
                    LocalizationService.HandleReply(ctx, "You do not have the required prestige level for that spell.");
                    return;
                }

                List<int> spells = Core.ParseConfigString(LevelingSystem.ClassSpellsMap[playerClass]);

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

    [Command(name: "changeClass", shortHand: "change", adminOnly: false, usage: ".change [Class]", description: "Change classes.")]
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

        if (Plugin.ChangeClassItem.Value != 0 && !HandleClassChangeItem(ctx, classes, steamId))
        {
            LocalizationService.HandleReply(ctx, $"You do not have the required item to change classes. ({new PrefabGUID(Plugin.ChangeClassItem.Value).GetPrefabName()}x{Plugin.ChangeClassItemQuantity.Value})");
            return;
        }

        RemoveClassBuffs(ctx, steamId);

        classes.Clear();
        UpdateClassData(character, parsedClassType, classes, steamId);
        LocalizationService.HandleReply(ctx, $"You have changed to <color=white>{parsedClassType}</color>");
    }
    [Command(name: "syncClassBuffs", shortHand: "scb", adminOnly: false, usage: ".scb", description: "Applies class buffs appropriately if not present.")]
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
            List<int> perks = LevelingSystem.GetClassBuffs(steamId);

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

    [Command(name: "listClasses", shortHand: "classes", adminOnly: false, usage: ".classes", description: "Sets player level.")]
    public static void ListClasses(ChatCommandContext ctx)
    {
        if (!SoftSynergies && !HardSynergies)
        {
            LocalizationService.HandleReply(ctx, "Classes are not enabled.");
            return;
        }

        string classTypes = string.Join(", ", Enum.GetNames(typeof(LevelingSystem.PlayerClasses)));
        LocalizationService.HandleReply(ctx, $"Available Classes: <color=white>{classTypes}</color>");
    }

    [Command(name: "listClassBuffs", shortHand: "lcb", adminOnly: false, usage: ".lcb <ClassType>", description: "Shows perks that can be gained from class.")]
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

            List<int> perks = Core.ParseConfigString(LevelingSystem.ClassPrestigeBuffsMap[playerClass]);

            if (perks.Count == 0)
            {
                LocalizationService.HandleReply(ctx, "Class buffs not found.");
                return;
            }

   


    [Command(name: "listClassSpells", shortHand: "lcs", adminOnly: false, usage: ".lcs <ClassType>", description: "Shows spells that can be gained from class.")]
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

            List<int> perks = Core.ParseConfigString(LevelingSystem.ClassSpellsMap[playerClass]);

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

    [Command(name: "listClassStats", shortHand: "stats", adminOnly: false, usage: ".stats [Class]", description: "Shows weapon and blood stats for a class.")]
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

    [Command(name: "playerPrestige", shortHand: "prestige", adminOnly: false, usage: ".prestige [PrestigeType]", description: "Handles player prestiging.")]
    public static void PrestigeCommand(ChatCommandContext ctx, string prestigeType)
    {
        if (!Prestige)
        {
            LocalizationService.HandleReply(ctx, "Prestiging is not enabled.");
            return;
        }

        if (!PrestigeSystem.TryParsePrestigeType(prestigeType, out var parsedPrestigeType))
        {
            LocalizationService.HandleReply(ctx, "Invalid prestige, use .lpp to see options.");
            return;
        }

        if ((SoftSynergies || HardSynergies) &&
            Core.DataStructures.PlayerClasses.TryGetValue(ctx.Event.User.PlatformId, out var classes) &&
            classes.Keys.Count == 0 &&
            parsedPrestigeType == PrestigeSystem.PrestigeType.Experience)
        {
            LocalizationService.HandleReply(ctx, "You must choose a class before prestiging in experience.");
            return;
        }

        var steamId = ctx.Event.User.PlatformId;
        var handler = PrestigeHandlerFactory.GetPrestigeHandler(parsedPrestigeType);

        if (handler == null)
        {
            LocalizationService.HandleReply(ctx, "Invalid prestige type.");
            return;
        }

        var xpData = handler.GetExperienceData(steamId);
        if (PrestigeSystem.CanPrestige(steamId, parsedPrestigeType, xpData.Key))
        {
            PrestigeSystem.PerformPrestige(ctx, steamId, parsedPrestigeType, handler);
        }
        else
        {
            LocalizationService.HandleReply(ctx, $"You have not reached the required level to prestige in <color=#90EE90>{parsedPrestigeType}</color>.");
        }
    }

    [Command(name: "setPlayerPrestige", shortHand: "spr", adminOnly: true, usage: ".spr [PlayerID] [PrestigeType] [Level]", description: "Sets the specified player to a certain level of prestige in a certain type of prestige.")]
    public static void SetPlayerPrestigeCommand(ChatCommandContext ctx, string name, string prestigeType, int level)
    {
        if (!Prestige)
        {
            LocalizationService.HandleReply(ctx, "Prestiging is not enabled.");
            return;
        }

        if (!PrestigeSystem.TryParsePrestigeType(prestigeType, out var parsedPrestigeType))
        {
            LocalizationService.HandleReply(ctx, "Invalid prestige type, use .lpp to see options.");
            return;
        }

        Entity userEntity = PlayerService.GetUserByName(name, true);
        ulong playerId = userEntity.Read<User>().PlatformId;

        if ((SoftSynergies || HardSynergies) &&
            Core.DataStructures.PlayerClasses.TryGetValue(playerId, out var classes) &&
            classes.Keys.Count == 0 &&
            parsedPrestigeType == PrestigeSystem.PrestigeType.Experience)
        {
            LocalizationService.HandleReply(ctx, "The player must choose a class before prestiging in experience.");
            return;
        }

        var handler = PrestigeHandlerFactory.GetPrestigeHandler(parsedPrestigeType);

        if (handler == null)
        {
            LocalizationService.HandleReply(ctx, "Invalid prestige type.");
            return;
        }

        if (!Core.DataStructures.PlayerPrestiges.TryGetValue(playerId, out var prestigeData))
        {
            prestigeData = [];
            Core.DataStructures.PlayerPrestiges[playerId] = prestigeData;
        }

        if (!prestigeData.ContainsKey(parsedPrestigeType))
        {
            prestigeData[parsedPrestigeType] = 0;
        }

        if (level > PrestigeSystem.PrestigeTypeToMaxPrestigeLevel[parsedPrestigeType])
        {
            LocalizationService.HandleReply(ctx, $"The maximum level for {parsedPrestigeType} prestige is {PrestigeSystem.PrestigeTypeToMaxPrestigeLevel[parsedPrestigeType]}.");
            return;
        }

        prestigeData[parsedPrestigeType] = level;
        handler.SaveChanges();

        // Apply effects based on the prestige type
        if (parsedPrestigeType == PrestigeSystem.PrestigeType.Experience)
        {
            PrestigeSystem.ApplyPrestigeBuffs(ctx, level);
            PrestigeSystem.ApplyExperiencePrestigeEffects(ctx, playerId, level);
        }
        else
        {
            PrestigeSystem.ApplyOtherPrestigeEffects(ctx, playerId, parsedPrestigeType, level);
        }

        LocalizationService.HandleReply(ctx, $"Player <color=green>{playerId}</color> has been set to level <color=white>{level}</color> in <color=#90EE90>{parsedPrestigeType}</color> prestige.");
    }

    [Command(name: "listPrestigeBuffs", shortHand: "lpb", adminOnly: false, usage: ".lpb", description: "Lists prestige buff names.")]
    public static void PrestigeBuffsCommand(ChatCommandContext ctx)
    {
        if (!Prestige)
        {
            LocalizationService.HandleReply(ctx, "Prestiging is not enabled.");
            return;
        }

        List<int> buffs = Core.ParseConfigString(Plugin.PrestigeBuffs.Value);

        if (buffs.Count == 0)
        {
            LocalizationService.HandleReply(ctx, "Prestiging buffs not found.");
            return;
        }

        var prestigeBuffs = buffs.Select((buff, index) =>
        {
            int level = index + 1;
            string prefab = new PrefabGUID(buff).LookupName();
            int prefabIndex = prefab.IndexOf("Prefab");
            if (prefabIndex != -1)
            {
                prefab = prefab[..prefabIndex].TrimEnd();
            }
            return $"<color=white>{prefab}</color> at prestige <color=yellow>{level}</color>";
        }).ToList();

        for (int i = 0; i < prestigeBuffs.Count; i += 4)
        {
            var batch = prestigeBuffs.Skip(i).Take(4);
            string replyMessage = string.Join(", ", batch);
            LocalizationService.HandleReply(ctx, replyMessage);
        }
    }
  
    [Command(name: "resetPrestige", shortHand: "rpr", adminOnly: true, usage: ".rpr [Name] [PrestigeType]", description: "Handles resetting prestiging.")]
    public static void ResetPrestige(ChatCommandContext ctx, string name, string prestigeType)
    {
        if (!Prestige)
        {
            LocalizationService.HandleReply(ctx, "Prestiging is not enabled.");
            return;
        }

        if (!PrestigeSystem.TryParsePrestigeType(prestigeType, out var parsedPrestigeType))
        {
            LocalizationService.HandleReply(ctx, "Invalid prestige, use .lpp to see options.");
            return;
        }

        Entity foundUserEntity = PlayerService.GetUserByName(name, true);

        if (foundUserEntity.Equals(Entity.Null))
        {
            LocalizationService.HandleReply(ctx, "Player not found...");
            return;
        }

        User foundUser = foundUserEntity.Read<User>();
        ulong steamId = foundUser.PlatformId;

        if (Core.DataStructures.PlayerPrestiges.TryGetValue(steamId, out var prestigeData) &&
            prestigeData.TryGetValue(parsedPrestigeType, out var prestigeLevel))
        {
            if (parsedPrestigeType == PrestigeSystem.PrestigeType.Experience)
            {
                PrestigeSystem.RemovePrestigeBuffs(ctx, prestigeLevel);
            }
            prestigeData[parsedPrestigeType] = 0;
            Core.DataStructures.SavePlayerPrestiges();
            LocalizationService.HandleReply(ctx, $"<color=#90EE90>{parsedPrestigeType}</color> prestige reset for <color=white>{foundUser.CharacterName}</color>.");
        }
    }

    [Command(name: "syncPrestigeBuffs", shortHand: "spb", adminOnly: false, usage: ".spb", description: "Applies prestige buffs appropriately if not present.")]
    public static void SyncPrestigeBuffsCommand(ChatCommandContext ctx)
    {
        if (!Prestige)
        {
            LocalizationService.HandleReply(ctx, "Prestiging is not enabled.");
            return;
        }

        var steamId = ctx.Event.User.PlatformId;

        if (Core.DataStructures.PlayerPrestiges.TryGetValue(steamId, out var prestigeData) &&
            prestigeData.TryGetValue(PrestigeSystem.PrestigeType.Experience, out var prestigeLevel) && prestigeLevel > 0)
        {
            PrestigeSystem.ApplyPrestigeBuffs(ctx, prestigeLevel);
            LocalizationService.HandleReply(ctx, "Prestige buffs applied.");
        }
        else
        {
            LocalizationService.HandleReply(ctx, $"You have not prestiged in <color=#90EE90>{PrestigeSystem.PrestigeType.Experience}</color>.");
        }
    }

    [Command(name: "getPrestige", shortHand: "gpr", adminOnly: false, usage: ".gpr [PrestigeType]", description: "Shows information about player's prestige status.")]
    public static void GetPrestigeCommand(ChatCommandContext ctx, string prestigeType)
    {
        if (!Prestige)
        {
            LocalizationService.HandleReply(ctx, "Prestiging is not enabled.");
            return;
        }

        if (!PrestigeSystem.TryParsePrestigeType(prestigeType, out var parsedPrestigeType))
        {
            LocalizationService.HandleReply(ctx, "Invalid prestige, use .lpp to see options.");
            return;
        }

        var steamId = ctx.Event.User.PlatformId;
        var handler = PrestigeHandlerFactory.GetPrestigeHandler(parsedPrestigeType);

        if (handler == null)
        {
            LocalizationService.HandleReply(ctx, "Invalid prestige type.");
            return;
        }

        var maxPrestigeLevel = PrestigeSystem.PrestigeTypeToMaxPrestigeLevel[parsedPrestigeType];
        if (Core.DataStructures.PlayerPrestiges.TryGetValue(steamId, out var prestigeData) &&
            prestigeData.TryGetValue(parsedPrestigeType, out var prestigeLevel) && prestigeLevel > 0)
        {
            PrestigeSystem.DisplayPrestigeInfo(ctx, steamId, parsedPrestigeType, prestigeLevel, maxPrestigeLevel);
        }
        else
        {
            LocalizationService.HandleReply(ctx, $"You have not prestiged in <color=#90EE90>{parsedPrestigeType}</color>.");
        }
    }

    [Command(name: "listPlayerPrestiges", shortHand: "lpp", adminOnly: false, usage: ".lpp", description: "Lists prestiges available.")]
    public static void ListPlayerPrestigeTypes(ChatCommandContext ctx)
    {
        if (!Prestige)
        {
            LocalizationService.HandleReply(ctx, "Prestiging is not enabled.");
            return;
        }
        string prestigeTypes = string.Join(", ", Enum.GetNames(typeof(PrestigeSystem.PrestigeType)));
        LocalizationService.HandleReply(ctx, $"Available Prestiges: <color=#90EE90>{prestigeTypes}</color>");
    }

    [Command(name: "togglePartyInvites", shortHand: "pinvites", adminOnly: false, usage: ".pinvites", description: "Toggles being able to be invited to parties, prevents damage and share exp.")]
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

    [Command(name: "partyAdd", shortHand: "pa", adminOnly: false, usage: ".pa [Player]", description: "Adds player to party.")]
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

    [Command(name: "partyRemove", shortHand: "pr", adminOnly: false, usage: ".pr [Player]", description: "Removes player from party.")]
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

    [Command(name: "listPartyMembers", shortHand: "lpm", adminOnly: false, usage: ".lpm", description: "Lists party members of your active party.")]
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

    [Command(name: "disbandParty", shortHand: "dparty", adminOnly: false, usage: ".dparty", description: "Disbands party.")]
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

    [Command(name: "leaveParty", shortHand: "lparty", adminOnly: false, usage: ".lparty", description: "Leaves party if in one.")]
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