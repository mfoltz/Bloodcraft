using Bloodcraft.Patches;
using Bloodcraft.Services;
using Bloodcraft.Systems.Experience;
using Bloodcraft.Systems.Leveling;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;
using static Bloodcraft.Systems.Experience.LevelingSystem;
using static Bloodcraft.Systems.Experience.LevelingSystem.AllianceUtilities;
using static Bloodcraft.Systems.Expertise.WeaponStats.WeaponStatManager;
using static Bloodcraft.Systems.Legacies.BloodStats.BloodStatManager;

namespace Bloodcraft.Commands;
internal static class LevelingCommands
{
    static readonly bool Leveling = Plugin.LevelingSystem.Value;
    static readonly bool SoftSynergies = Plugin.SoftSynergies.Value;
    static readonly bool HardSynergies = Plugin.HardSynergies.Value;
    static readonly bool ShiftSlot = Plugin.ShiftSlot.Value;
    static readonly bool PlayerAlliances = Plugin.PlayerAlliances.Value;
    static readonly bool ClanAlliances = false;
    static readonly bool Prestige = Plugin.PrestigeSystem.Value;
    static readonly int MaxPlayerLevel = Plugin.MaxPlayerLevel.Value;

    [Command(name: "prepareForTheHunt", shortHand: "prepare", adminOnly: false, usage: ".prepare", description: "Completes GettingReadyForTheHunt if not already completed.")]
    public static void QuickStartCommand(ChatCommandContext ctx)
    {
        if (!Leveling)
        {
            LocalizationService.HandleReply(ctx, "Leveling is not enabled.");
            return;
        }
        EntityCommandBuffer entityCommandBuffer = Core.EntityCommandBufferSystem.CreateCommandBuffer();
        PrefabGUID achievementPrefabGUID = new(560247139); // Journal_GettingReadyForTheHunt
        Entity userEntity = ctx.Event.SenderUserEntity;
        Entity characterEntity = ctx.Event.SenderCharacterEntity;
        Entity achievementOwnerEntity = userEntity.Read<AchievementOwner>().Entity._Entity;
        Core.ClaimAchievementSystem.CompleteAchievement(entityCommandBuffer, achievementPrefabGUID, userEntity, characterEntity, achievementOwnerEntity, false, true);
        LocalizationService.HandleReply(ctx, "You are now prepared for the hunt.");
    }

    [Command(name: "logLevelingProgress", shortHand: "log l", adminOnly: false, usage: ".log l", description: "Toggles leveling progress logging.")]
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

    [Command(name: "getLevelingProgress", shortHand: "get l", adminOnly: false, usage: ".get l", description: "Display current leveling progress.")]
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
            int progress = (int)(levelKvp.Value - LevelingSystem.ConvertLevelToXp(level));
            int percent = LevelingSystem.GetLevelProgress(steamId);
            LocalizationService.HandleReply(ctx, $"You're level [<color=white>{level}</color>] and have <color=yellow>{progress}</color> <color=#FFC0CB>experience</color> (<color=white>{percent}%</color>)");
        }
        else
        {
            LocalizationService.HandleReply(ctx, "No experience yet.");
        }
    }

    [Command(name: "setLevel", shortHand: "sl", adminOnly: true, usage: ".sl [Player] [Level]", description: "Sets player level.")]
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
            var xpData = new KeyValuePair<int, float>(level, LevelingSystem.ConvertLevelToXp(level));
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

            List<int> perks = LevelingSystem.GetClassBuffs(steamId);

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

            List<int> perks = LevelingSystem.GetClassSpells(steamId);

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

        LocalizationService.HandleReply(ctx, $"Player {playerId} has been set to level {level} in {parsedPrestigeType} prestige.");
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

        for (int i = 0; i < prestigeBuffs.Count; i += 6)
        {
            var batch = prestigeBuffs.Skip(i).Take(6);
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

    [Command(name: "toggleAllianceInvites", shortHand: "invites", adminOnly: false, usage: ".invites", description: "Toggles being able to be invited to an alliance. Allowed in raids of allied players and share exp if applicable.")]
    public static void ToggleAllianceInvitesCommand(ChatCommandContext ctx)
    {
        if (!PlayerAlliances)
        {
            LocalizationService.HandleReply(ctx, "Alliances are not enabled.");
            return;
        }

        ulong SteamID = ctx.Event.User.PlatformId;
        Entity ownerClanEntity = ctx.Event.User.ClanEntity._Entity;
        string name = ctx.Event.User.CharacterName.Value;

        if (ClanAlliances && ownerClanEntity.Equals(Entity.Null) || !Core.EntityManager.Exists(ownerClanEntity))
        {
            LocalizationService.HandleReply(ctx, "You must be the leader of a clan to toggle alliance invites.");
            return;
        }
        else if (ClanAlliances)
        {
            Entity userEntity = ctx.Event.SenderUserEntity;
            if (userEntity.TryGetComponent(out ClanRole clanRole) && !clanRole.Value.Equals(ClanRoleEnum.Leader))
            {
                LocalizationService.HandleReply(ctx, "You must be the leader of a clan to toggle alliance invites.");
                return;
            }
        }

        if (Core.DataStructures.PlayerAlliances.Any(kvp => kvp.Value.Contains(name)))
        {
            LocalizationService.HandleReply(ctx, "You are already in an alliance. Leave or disband if owned before enabling invites.");
            return;
        }

        if (Core.DataStructures.PlayerBools.TryGetValue(SteamID, out var bools))
        {
            bools["Grouping"] = !bools["Grouping"];
        }
        Core.DataStructures.SavePlayerBools();
        LocalizationService.HandleReply(ctx, $"Alliance invites {(bools["Grouping"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
    }

    [Command(name: "allianceAdd", shortHand: "aa", adminOnly: false, usage: ".aa [Player/Clan]", description: "Adds player/clan to alliance if invites are toggled (if clan based owner of clan must toggle).")]
    public static void AllianceAddCommand(ChatCommandContext ctx, string name)
    {  
        if (!PlayerAlliances)
        {
            LocalizationService.HandleReply(ctx, "Alliances are not enabled.");
            return;
        }

        ulong ownerId = ctx.Event.User.PlatformId;
        Entity ownerClanEntity = ctx.Event.User.ClanEntity._Entity;

        if (ClanAlliances)
        {
            if (CheckClanLeadership(ctx, ownerClanEntity))
            {
                LocalizationService.HandleReply(ctx, "You must be the leader of a clan to form an alliance.");
                return;
            }

            HandleClanAlliance(ctx, ownerId, name);
        }
        else
        {
            HandlePlayerAlliance(ctx, ownerId, name);
        }
    }

    [Command(name: "allianceRemove", shortHand: "ar", adminOnly: false, usage: ".ar [Player/Clan]", description: "Removes player or clan from alliance.")]
    public static void AllianceRemoveCommand(ChatCommandContext ctx, string name)
    {
        if (!PlayerAlliances)
        {
            LocalizationService.HandleReply(ctx, "Alliances are not enabled.");
            return;
        }

        ulong ownerId = ctx.Event.User.PlatformId;
        Entity ownerClanEntity = ctx.Event.User.ClanEntity._Entity;

        if (ClanAlliances && CheckClanLeadership(ctx, ownerClanEntity))
        {
            LocalizationService.HandleReply(ctx, "You must be the leader of a clan to remove clans from an alliance.");
            return;
        }

        if (!Core.DataStructures.PlayerAlliances.ContainsKey(ownerId))
        {
            LocalizationService.HandleReply(ctx, "You don't have an alliance.");
            return;
        }

        HashSet<string> alliance = Core.DataStructures.PlayerAlliances[ownerId]; // check size and if player is already present in group before adding

        if (ClanAlliances)
        {
            RemoveClanFromAlliance(ctx, alliance, name);
        }
        else
        {
            RemovePlayerFromAlliance(ctx, alliance, name);
        }
    }

    [Command(name: "listAllianceMembers", shortHand: "lam", adminOnly: false, usage: ".lam [Player]", description: "Lists alliance members of your alliance or the alliance you are in or the members in the alliance of the player entered if found.")]
    public static void AllianceMembersCommand(ChatCommandContext ctx, string name = "")
    {
        if (!PlayerAlliances)
        {
            LocalizationService.HandleReply(ctx, "Alliances are not enabled.");
            return;
        }        

        Dictionary<ulong, HashSet<string>> playerAlliances = Core.DataStructures.PlayerAlliances;

        if (string.IsNullOrEmpty(name))
        {
            ListPersonalAllianceMembers(ctx, playerAlliances);
        }
        else
        {
            ListAllianceMembersByName(ctx, name, playerAlliances);
        }
    }

    [Command(name: "allianceDisband", shortHand: "disband", adminOnly: false, usage: ".disband", description: "Disbands alliance.")]
    public static void DisbandAllianceCommand(ChatCommandContext ctx)
    {
        if (!PlayerAlliances)
        {
            LocalizationService.HandleReply(ctx, "Alliances are not enabled.");
            return;
        }

        ulong ownerId = ctx.Event.User.PlatformId;
        Entity ownerClanEntity = ctx.Event.User.ClanEntity._Entity;

        if (ClanAlliances && CheckClanLeadership(ctx, ownerClanEntity))
        {
            LocalizationService.HandleReply(ctx, "You must be the leader of your clan to disband the alliance.");
            return;
        }

        if (!Core.DataStructures.PlayerAlliances.ContainsKey(ownerId)) 
        {
            LocalizationService.HandleReply(ctx, "You don't have an alliance to disband.");
            return;
        }
       
        Core.DataStructures.PlayerAlliances.Remove(ownerId);
        LocalizationService.HandleReply(ctx, "Alliance disbanded.");
        Core.DataStructures.SavePlayerAlliances();   
    }

    [Command(name: "leaveAlliance", shortHand: "leave", adminOnly: false, usage: ".leave", description: "Leaves alliance if in one.")]
    public static void LeaveAllianceCommand(ChatCommandContext ctx)
    {
        if (!PlayerAlliances)
        {
            LocalizationService.HandleReply(ctx, "Alliances are not enabled.");
            return;
        }

        ulong ownerId = ctx.Event.User.PlatformId;
        Entity ownerClanEntity = ctx.Event.User.ClanEntity._Entity;
        string playerName = ctx.Event.User.CharacterName.Value;

        if (ClanAlliances && CheckClanLeadership(ctx, ownerClanEntity))
        {
            LocalizationService.HandleReply(ctx, "You must be the leader of a clan to leave an alliance.");
            return;
        }

        if (Core.DataStructures.PlayerAlliances.ContainsKey(ownerId))
        {
            LocalizationService.HandleReply(ctx, "You can't leave your own alliance. Disband it instead.");
            return;
        }

        if (ClanAlliances)
        {
            var alliance = Core.DataStructures.PlayerAlliances.Values.FirstOrDefault(set => set.Contains(playerName));
            if (alliance != null)
            {
                RemoveClanFromAlliance(ctx, alliance, ownerClanEntity.Read<ClanTeam>().Name.Value);
            }
            else
            {
                LocalizationService.HandleReply(ctx, "Your clan is not in an alliance.");
            }
        }
        else
        {
            var alliance = Core.DataStructures.PlayerAlliances.Values.FirstOrDefault(set => set.Contains(playerName));
            if (alliance != null)
            {
                RemovePlayerFromAlliance(ctx, alliance, playerName);
            }
            else
            {
                LocalizationService.HandleReply(ctx, "You're not in an alliance.");
            }    
        }
    }
}