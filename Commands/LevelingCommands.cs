using Bloodcraft.Patches;
using Bloodcraft.Services;
using Bloodcraft.Systems.Experience;
using Bloodcraft.Systems.Leveling;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.DebugDisplay;
using Unity.Entities;
using Unity.Transforms;
using VampireCommandFramework;
using static Bloodcraft.Services.LocalizationService;
using static Bloodcraft.Services.PlayerService;
using static Bloodcraft.Systems.Experience.LevelingSystem;

namespace Bloodcraft.Commands;
internal static class LevelingCommands
{
    [Command(name: "quickStart", shortHand: "start", adminOnly: false, usage: ".start", description: "Completes GettingReadyForTheHunt if not already completed.")]
    public static void QuickStartCommand(ChatCommandContext ctx)
    {
        if (!Plugin.LevelingSystem.Value)
        {
            HandleReply(ctx, "Leveling is not enabled.");
            return;
        }
        EntityCommandBuffer entityCommandBuffer = Core.EntityCommandBufferSystem.CreateCommandBuffer();
        PrefabGUID achievementPrefabGUID = new(560247139); // Journal_GettingReadyForTheHunt
        Entity userEntity = ctx.Event.SenderUserEntity;
        Entity characterEntity = ctx.Event.SenderCharacterEntity;
        Entity achievementOwnerEntity = userEntity.Read<AchievementOwner>().Entity._Entity;
        Core.ClaimAchievementSystem.CompleteAchievement(entityCommandBuffer, achievementPrefabGUID, userEntity, characterEntity, achievementOwnerEntity, false, true);
        HandleReply(ctx, "You are now prepared for the hunt.");
    }

    [Command(name: "logLevelingProgress", shortHand: "log l", adminOnly: false, usage: ".log l", description: "Toggles leveling progress logging.")]
    public static void LogExperienceCommand(ChatCommandContext ctx)
    {
        if (!Plugin.LevelingSystem.Value)
        {
            HandleReply(ctx, "Leveling is not enabled.");
            return;
        }
        var SteamID = ctx.Event.User.PlatformId;

        if (Core.DataStructures.PlayerBools.TryGetValue(SteamID, out var bools))
        {
            bools["ExperienceLogging"] = !bools["ExperienceLogging"];
        }
        Core.DataStructures.SavePlayerBools();
        HandleReply(ctx, $"Leveling experience logging {(bools["ExperienceLogging"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
    }

    [Command(name: "getLevelingProgress", shortHand: "get l", adminOnly: false, usage: ".get l", description: "Display current leveling progress.")]
    public static void GetLevelCommand(ChatCommandContext ctx)
    {
        if (!Plugin.LevelingSystem.Value)
        {
            HandleReply(ctx, "Leveling is not enabled.");
            return;
        }
        ulong steamId = ctx.Event.User.PlatformId;
        if (Core.DataStructures.PlayerExperience.TryGetValue(steamId, out var Leveling))
        {
            int level = Leveling.Key;
            int progress = (int)(Leveling.Value - LevelingSystem.ConvertLevelToXp(level));
            int percent = LevelingSystem.GetLevelProgress(steamId);
            HandleReply(ctx, $"You're level [<color=white>{level}</color>] and have <color=yellow>{progress}</color> <color=#FFC0CB>experience</color> (<color=white>{percent}%</color>)");
        }
        else
        {
            HandleReply(ctx, "No experience yet.");
        }
    }

    [Command(name: "setLevel", shortHand: "sl", adminOnly: true, usage: ".sl [Player] [Level]", description: "Sets player level.")]
    public static void SetLevelCommand(ChatCommandContext ctx, string name, int level)
    {
        if (!Plugin.LevelingSystem.Value)
        {
            HandleReply(ctx, "Leveling is not enabled.");
            return;
        }

        Entity foundUserEntity = GetUserByName(name, true);
        if (foundUserEntity.Equals(Entity.Null))
        {
            HandleReply(ctx, "Player not found...");
            return;
        }
        User foundUser = foundUserEntity.Read<User>();

        if (level < 0 || level > Plugin.MaxPlayerLevel.Value)
        {
            HandleReply(ctx, $"Level must be between 0 and {Plugin.MaxPlayerLevel.Value}");
            return;
        }
        ulong steamId = foundUser.PlatformId;
        if (Core.DataStructures.PlayerExperience.TryGetValue(steamId, out var _))
        {
            var xpData = new KeyValuePair<int, float>(level, LevelingSystem.ConvertLevelToXp(level));
            Core.DataStructures.PlayerExperience[steamId] = xpData;
            Core.DataStructures.SavePlayerExperience();
            GearOverride.SetLevel(foundUser.LocalCharacter._Entity);
            HandleReply(ctx, $"Level set to <color=white>{level}</color> for <color=green>{foundUser.CharacterName}</color>");
        }
        else
        {
            HandleReply(ctx, "No experience found.");
        }
    }

    [Command(name: "chooseClass", shortHand: "cc", adminOnly: false, usage: ".cc [Class]", description: "Choose class.")]
    public static void ClassChoiceCommand(ChatCommandContext ctx, string className)
    {
        if (!Plugin.SoftSynergies.Value && !Plugin.HardSynergies.Value)
        {
            HandleReply(ctx, "Classes are not enabled.");
            return;
        }

        if (!TryParseClassName(className, out var parsedClassType))
        {
            HandleReply(ctx, "Invalid class, use .classes to see options.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;

        if (Core.DataStructures.PlayerClasses.TryGetValue(steamId, out var classes))
        {
            if (classes.Keys.Count > 0)
            {
                HandleReply(ctx, "You have already chosen a class.");
                return;
            }
            UpdateClassData(ctx.Event.SenderCharacterEntity, parsedClassType, classes, steamId);
            HandleReply(ctx, $"You have chosen <color=white>{parsedClassType}</color>");
        }
    }

    [Command(name: "chooseClassSpell", shortHand: "cs", adminOnly: false, usage: ".cs [#]", description: "Sets shift spell for class if prestige level is high enough.")]
    public static void ChooseClassSpell(ChatCommandContext ctx, int choice)
    {
        if (!Plugin.SoftSynergies.Value && !Plugin.HardSynergies.Value)
        {
            HandleReply(ctx, "Classes are not enabled.");
            return;
        }
        if (!Plugin.ShiftSlots.Value)
        {
            HandleReply(ctx, "Shift slots are not enabled for class spells.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;

        if (Core.DataStructures.PlayerClasses.TryGetValue(steamId, out var classes))
        {
            if (classes.Keys.Count == 0)
            {
                HandleReply(ctx, "You haven't chosen a class yet.");
                return;
            }
            PlayerClasses playerClass = classes.Keys.FirstOrDefault();
            if (Core.DataStructures.PlayerPrestiges.TryGetValue(steamId, out var prestigeData) && prestigeData.TryGetValue(PrestigeSystem.PrestigeType.Experience, out var prestigeLevel))
            {
                if (prestigeLevel < Core.ParseConfigString(Plugin.PrestigeLevelsToUnlockClassSpells.Value)[choice - 1])
                {
                    HandleReply(ctx, "You do not have the required prestige level for that spell.");
                    return;
                }

                List<int> spells = Core.ParseConfigString(LevelingSystem.ClassSpellsMap[playerClass]);

                if (spells.Count == 0)
                {
                    HandleReply(ctx, "No spells found for class.");
                    return;
                }

                if (choice < 1 || choice > spells.Count)
                {
                    HandleReply(ctx, $"Invalid spell choice. (Use 1-{spells.Count})");
                    return;
                }

                if (Core.DataStructures.PlayerSpells.TryGetValue(steamId, out var spellsData))
                {
                    spellsData.ClassSpell = spells[choice - 1];
                    Core.DataStructures.PlayerSpells[steamId] = spellsData;
                    Core.DataStructures.SavePlayerSpells();

                    HandleReply(ctx, $"You have chosen spell <color=#CBC3E3>{new PrefabGUID(spells[choice - 1]).LookupName()}</color> from <color=white>{playerClass}</color>, it will be available on weapons and unarmed if .shift is enabled.");
                }
            }
            else
            {
                HandleReply(ctx, "You haven't prestiged in leveling yet.");
            }
        }
        else
        {
            HandleReply(ctx, "You haven't chosen a class yet.");
        }
    }

    [Command(name: "changeClass", shortHand: "change", adminOnly: false, usage: ".change [Class]", description: "Change classes.")]
    public static void ClassChangeCommand(ChatCommandContext ctx, string className)
    {
        if (!Plugin.SoftSynergies.Value && !Plugin.HardSynergies.Value)
        {
            HandleReply(ctx, "Classes are not enabled.");
            return;
        }

        if (!TryParseClassName(className, out var parsedClassType))
        {
            HandleReply(ctx, "Invalid class, use .classes to see options.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;
        Entity character = ctx.Event.SenderCharacterEntity;

        if (!Core.DataStructures.PlayerClasses.TryGetValue(steamId, out var classes))
        {
            HandleReply(ctx, "You haven't chosen a class yet.");
            return;
        }

        if (Plugin.ChangeClassItem.Value != 0 && !HandleClassChangeItem(ctx, classes, steamId))
        {
            HandleReply(ctx, $"You do not have the required item to change classes. ({new PrefabGUID(Plugin.ChangeClassItem.Value).GetPrefabName()}x{Plugin.ChangeClassItemQuantity.Value})");
            return;
        }

        RemoveClassBuffs(ctx, steamId);

        classes.Clear();
        UpdateClassData(character, parsedClassType, classes, steamId);
        HandleReply(ctx, $"You have changed to <color=white>{parsedClassType}</color>");
    }

    [Command(name: "listClasses", shortHand: "classes", adminOnly: false, usage: ".classes", description: "Sets player level.")]
    public static void ListClasses(ChatCommandContext ctx)
    {
        if (!Plugin.SoftSynergies.Value && !Plugin.HardSynergies.Value)
        {
            HandleReply(ctx, "Classes are not enabled.");
            return;
        }

        string classTypes = string.Join(", ", Enum.GetNames(typeof(LevelingSystem.PlayerClasses)));
        HandleReply(ctx, $"Available Classes: <color=white>{classTypes}</color>");
    }

    [Command(name: "listClassBuffs", shortHand: "lcb", adminOnly: false, usage: ".lcb", description: "Shows perks that can be gained from class.")]
    public static void ClassPerks(ChatCommandContext ctx)
    {
        if (!Plugin.SoftSynergies.Value && !Plugin.HardSynergies.Value)
        {
            HandleReply(ctx, "Classes are not enabled.");
            return;
        }
        ulong steamId = ctx.Event.User.PlatformId;
        if (Core.DataStructures.PlayerClasses.TryGetValue(steamId, out var classes))
        {
            if (classes.Keys.Count == 0)
            {
                HandleReply(ctx, "You haven't chosen a class yet.");
                return;
            }
            PlayerClasses playerClass = classes.Keys.FirstOrDefault();
            List<int> perks = LevelingSystem.GetClassBuffs(steamId);

            if (perks.Count == 0)
            {
                HandleReply(ctx, "Class buffs not found.");
                return;
            }

            int step = Plugin.MaxPlayerLevel.Value / perks.Count;

            string replyMessage = string.Join(", ", perks.Select((perk, index) =>
            {
                int level = (index + 1) * step;
                return $"<color=white>{new PrefabGUID(perk).LookupName()}</color> at level <color=yellow>{level}</color>";
            }));
            HandleReply(ctx, $"{playerClass} perks: {replyMessage}");
        }
        else
        {
            HandleReply(ctx, "You haven't chosen a class yet.");
        }
    }

    [Command(name: "listClassSpells", shortHand: "lcs", adminOnly: false, usage: ".lcs", description: "Shows perks that can be gained from class.")]
    public static void ListClassSpells(ChatCommandContext ctx)
    {
        if (!Plugin.SoftSynergies.Value && !Plugin.HardSynergies.Value)
        {
            HandleReply(ctx, "Classes are not enabled.");
            return;
        }
        ulong steamId = ctx.Event.User.PlatformId;
        if (Core.DataStructures.PlayerClasses.TryGetValue(steamId, out var classes))
        {
            if (classes.Keys.Count == 0)
            {
                HandleReply(ctx, "You haven't chosen a class yet.");
                return;
            }
            PlayerClasses playerClass = classes.Keys.FirstOrDefault();
            List<int> perks = LevelingSystem.GetClassSpells(steamId);
            string replyMessage = string.Join("", perks.Select(perk => $"<color=white>{new PrefabGUID(perk).LookupName()}</color>"));
            HandleReply(ctx, $"{playerClass} spells: {replyMessage}");
        }
        else
        {
            HandleReply(ctx, "You haven't chosen a class yet.");
        }
    }

    [Command(name: "playerPrestige", shortHand: "prestige", adminOnly: false, usage: ".prestige [PrestigeType]", description: "Handles player prestiging.")]
    public static void PrestigeCommand(ChatCommandContext ctx, string prestigeType)
    {
        if (!Plugin.PrestigeSystem.Value)
        {
            HandleReply(ctx, "Prestiging is not enabled.");
            return;
        }

        if (!PrestigeSystem.TryParsePrestigeType(prestigeType, out var parsedPrestigeType))
        {
            HandleReply(ctx, "Invalid prestige, use .lpp to see options.");
            return;
        }

        if ((Plugin.SoftSynergies.Value || Plugin.HardSynergies.Value) &&
            Core.DataStructures.PlayerClasses.TryGetValue(ctx.Event.User.PlatformId, out var classes) &&
            classes.Keys.Count == 0 &&
            parsedPrestigeType == PrestigeSystem.PrestigeType.Experience)
        {
            HandleReply(ctx, "You must choose a class before prestiging in experience.");
            return;
        }

        var steamId = ctx.Event.User.PlatformId;
        var handler = PrestigeHandlerFactory.GetPrestigeHandler(parsedPrestigeType);

        if (handler == null)
        {
            HandleReply(ctx, "Invalid prestige type.");
            return;
        }

        var xpData = handler.GetExperienceData(steamId);
        if (PrestigeSystem.CanPrestige(steamId, parsedPrestigeType, xpData.Key))
        {
            PrestigeSystem.PerformPrestige(ctx, steamId, parsedPrestigeType, handler);
        }
        else
        {
            HandleReply(ctx, $"You have not reached the required level to prestige in <color=#90EE90>{parsedPrestigeType}</color>.");
        }
    }

    [Command(name: "listPrestigeBuffs", shortHand: "lpb", adminOnly: false, usage: ".lpb", description: "Lists prestige buff names.")]
    public static void PrestigeBuffsCommand(ChatCommandContext ctx)
    {
        if (!Plugin.PrestigeSystem.Value)
        {
            HandleReply(ctx, "Prestiging is not enabled.");
            return;
        }

        List<int> buffs = Core.ParseConfigString(Plugin.PrestigeBuffs.Value);

        if (buffs.Count == 0)
        {
            HandleReply(ctx, "Prestiging buffs not found.");
            return;
        }
        string replyMessage = string.Join(", ", buffs.Select((buff, index) =>
        {
            int level = index++;
            return $"<color=white>{new PrefabGUID(buff).LookupName()}</color> at prestige <color=yellow>{level}</color>";
        }));
        HandleReply(ctx, replyMessage);
    }

  
    [Command(name: "resetPrestige", shortHand: "rpr", adminOnly: true, usage: ".rpr [Name] [PrestigeType]", description: "Handles resetting prestiging.")]
    public static void ResetPrestige(ChatCommandContext ctx, string name, string prestigeType)
    {
        if (!Plugin.PrestigeSystem.Value)
        {
            HandleReply(ctx, "Prestiging is not enabled.");
            return;
        }

        if (!PrestigeSystem.TryParsePrestigeType(prestigeType, out var parsedPrestigeType))
        {
            HandleReply(ctx, "Invalid prestige, use .lpp to see options.");
            return;
        }

        Entity foundUserEntity = GetUserByName(name, true);

        if (foundUserEntity.Equals(Entity.Null))
        {
            HandleReply(ctx, "Player not found...");
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
            HandleReply(ctx, $"<color=#90EE90>{parsedPrestigeType}</color> prestige reset for <color=white>{foundUser.CharacterName}</color>.");
        }
    }
    

    [Command(name: "getPrestige", shortHand: "gpr", adminOnly: false, usage: ".gpr [PrestigeType]", description: "Shows information about player's prestige status.")]
    public unsafe static void GetPrestigeCommand(ChatCommandContext ctx, string prestigeType)
    {
        if (!Plugin.PrestigeSystem.Value)
        {
            HandleReply(ctx, "Prestiging is not enabled.");
            return;
        }

        if (!PrestigeSystem.TryParsePrestigeType(prestigeType, out var parsedPrestigeType))
        {
            HandleReply(ctx, "Invalid prestige, use .lpp to see options.");
            return;
        }

        var steamId = ctx.Event.User.PlatformId;
        var handler = PrestigeHandlerFactory.GetPrestigeHandler(parsedPrestigeType);

        if (handler == null)
        {
            HandleReply(ctx, "Invalid prestige type.");
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
            HandleReply(ctx, $"You have not prestiged in <color=#90EE90>{parsedPrestigeType}</color>.");
        }
    }

    [Command(name: "listPlayerPrestiges", shortHand: "lpp", adminOnly: false, usage: ".lpp", description: "Lists prestiges available.")]
    public static void ListPlayerPrestigeTypes(ChatCommandContext ctx)
    {
        if (!Plugin.PrestigeSystem.Value)
        {
            HandleReply(ctx, "Prestiging is not enabled.");
            return;
        }
        string prestigeTypes = string.Join(", ", Enum.GetNames(typeof(PrestigeSystem.PrestigeType)));
        HandleReply(ctx, $"Available Prestiges: <color=#90EE90>{prestigeTypes}</color>");
    }

    [Command(name: "toggleAlliances", shortHand: "invites", adminOnly: false, usage: ".invites", description: "Toggles being able to be invited to an alliance. Allowed in raids of allied players and share exp if applicable.")]
    public static void ToggleAlliancesCommand(ChatCommandContext ctx)
    {
        if (!Plugin.PlayerAlliances.Value)
        {
            HandleReply(ctx, "Alliances are not enabled.");
            return;
        }

        var SteamID = ctx.Event.User.PlatformId;

        if (Core.DataStructures.PlayerBools.TryGetValue(SteamID, out var bools))
        {
            bools["Grouping"] = !bools["Grouping"];
        }
        Core.DataStructures.SavePlayerBools();
        HandleReply(ctx, $"Alliance invites {(bools["Grouping"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
    }

    [Command(name: "allianceAdd", shortHand: "aa", adminOnly: false, usage: ".aa [Player]", description: "Adds player to alliance if invites are toggled.")]
    public static void AllianceAddCommand(ChatCommandContext ctx, string name)
    {
        if (!Plugin.PlayerAlliances.Value)
        {
            HandleReply(ctx, "Alliances are not enabled.");
            return;
        }

        ulong ownerId = ctx.Event.User.PlatformId;
        Entity ownerClanEntity = ctx.Event.User.ClanEntity._Entity;

        if (ownerClanEntity.Equals(Entity.Null) || !Core.EntityManager.Exists(ownerClanEntity))
        {
            HandleReply(ctx, "You must be in a clan to invite players to an alliance.");
            return;
        }

        Entity foundUserEntity = GetUserByName(name);
        if (foundUserEntity.Equals(Entity.Null))
        {
            HandleReply(ctx, "Player not found...");
            return;
        }

        User foundUser = foundUserEntity.Read<User>();
        if (foundUser.PlatformId == ownerId)
        {
            HandleReply(ctx, "Player not found...");
            return;
        }

        string playerName = foundUser.CharacterName.Value;
        if (Core.DataStructures.PlayerBools.TryGetValue(foundUser.PlatformId, out var bools) && bools["Grouping"] && !Core.DataStructures.PlayerAlliances.ContainsKey(foundUser.PlatformId)) // get consent, make sure they don't have their own group made first
        {
            if (!Core.DataStructures.PlayerAlliances.ContainsKey(ownerId)) // check if inviter has a group, make one if not
            {
                Core.DataStructures.PlayerAlliances[ownerId] = [];
            }

            HashSet<string> alliance = Core.DataStructures.PlayerAlliances[ownerId]; // check size and if player is already present in group before adding

            if (alliance.Count < Plugin.MaxAllianceSize.Value && !alliance.Contains(playerName))
            {
                alliance.Add(playerName);
                Core.DataStructures.SavePlayerAlliances();
                HandleReply(ctx, $"<color=green>{foundUser.CharacterName.Value}</color> added to alliance.");
            }
            else
            {
                HandleReply(ctx, $"Alliance is full or <color=green>{foundUser.CharacterName.Value}</color> is already in the alliance.");
            }
        }
        else
        {
            HandleReply(ctx, $"<color=green>{foundUser.CharacterName.Value}</color> does not have alliances enabled or they are the owner of an alliance.");
        }
    }

    [Command(name: "allianceRemove", shortHand: "ar", adminOnly: false, usage: ".ar [Player]", description: "Removes player from alliance.")]
    public static void AllianceRemoveCommand(ChatCommandContext ctx, string name)
    {
        if (!Plugin.PlayerAlliances.Value)
        {
            HandleReply(ctx, "Alliances are not enabled.");
            return;
        }

        ulong ownerId = ctx.Event.User.PlatformId;

        if (!Core.DataStructures.PlayerAlliances.ContainsKey(ownerId))
        {
            HandleReply(ctx, "You don't have an alliance.");
            return;
        }
        /*
        Entity foundUserEntity = GetUserByName(name, true);

        if (foundUserEntity.Equals(Entity.Null))
        {
            HandleReply(ctx, "Player not found...");
            return;
        }

        User foundUser = foundUserEntity.Read<User>();
        string userName = foundUser.CharacterName.Value;
        //Entity characterToRemove = foundUserEntity;
        */
        HashSet<string> alliance = Core.DataStructures.PlayerAlliances[ownerId]; // check size and if player is already present in group before adding

        if (alliance.Remove(alliance.FirstOrDefault(n => n.Equals(name, StringComparison.OrdinalIgnoreCase))))
        {
            alliance.Remove(name);
            Core.DataStructures.SavePlayerAlliances();                
            HandleReply(ctx, $"<color=green>{char.ToUpper(name[0]) + name[1..].ToLower()}</color> removed from alliance.");
        }
        else
        {
            HandleReply(ctx, $"<color=green>{char.ToUpper(name[0]) + name[1..].ToLower()}</color> is not in the alliance and therefore cannot be removed.");
        }
    }

    [Command(name: "listAllianceMembers", shortHand: "lam", adminOnly: false, usage: ".lam", description: "Lists alliance members of your alliance or the alliance you are in.")]
    public static void AllianceMembersCommand(ChatCommandContext ctx, string name = "")
    {
        if (!Plugin.PlayerAlliances.Value)
        {
            HandleReply(ctx, "Alliances are not enabled.");
            return;
        }

        ulong ownerId = ctx.Event.User.PlatformId;
        string playerName = ctx.Event.User.CharacterName.Value;

        Dictionary<ulong, HashSet<string>> playerAlliances = Core.DataStructures.PlayerAlliances;
        HashSet<string> members = playerAlliances.ContainsKey(ownerId) ? playerAlliances[ownerId] : playerAlliances.Where(groupEntry => groupEntry.Value.Contains(playerName)).SelectMany(groupEntry => groupEntry.Value).ToHashSet();

        string replyMessage = members.Count > 0 ? string.Join(", ", members.Select(member => $"<color=green>{member}</color>")): "No members in alliance."; 
        HandleReply(ctx, replyMessage);
    }

    [Command(name: "allianceDisband", shortHand: "disband", adminOnly: false, usage: ".disband", description: "Disbands alliance.")]
    public static void DisbandAllianceCommand(ChatCommandContext ctx)
    {
        if (!Plugin.PlayerAlliances.Value)
        {
            HandleReply(ctx, "Alliances are not enabled.");
            return;
        }

        ulong ownerId = ctx.Event.User.PlatformId;

        if (!Core.DataStructures.PlayerAlliances.ContainsKey(ownerId)) 
        {
            HandleReply(ctx, "You don't have an alliance to disband.");
            return;
        }
        else
        {
            Core.DataStructures.PlayerAlliances.Remove(ownerId);
            HandleReply(ctx, "Alliance disbanded.");
            Core.DataStructures.SavePlayerAlliances();
        }
    }

    
    /*
    [Command(name: "bufftest", shortHand: "bufftest", adminOnly: true, usage: ".bufftest", description: "buff test")]
    public static void TestCommand(ChatCommandContext ctx)
    {
      
        DebugEventsSystem debugEventsSystem = Core.DebugEventsSystem;
        ServerGameManager serverGameManager = Core.ServerGameManager;
        ApplyBuffDebugEvent applyBuffDebugEvent = new()
        {
            BuffPrefabGUID = new(-429891372) // mugging powersurge for it's components, prefectly ethical
        };
        FromCharacter fromCharacter = new()
        {
            Character = ctx.Event.SenderCharacterEntity,
            User = ctx.Event.SenderUserEntity
        };


        debugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
        if (serverGameManager.TryGetBuff(ctx.Event.SenderCharacterEntity, applyBuffDebugEvent.BuffPrefabGUID.ToIdentifier(), out Entity firstBuff)) // if present, modify based on prestige level
        {
            Core.Log.LogInfo($"Applied {applyBuffDebugEvent.BuffPrefabGUID.LookupName()} for class buff, modifying...");

            Buff buff = firstBuff.Read<Buff>();
            buff.BuffType = BuffType.Parallel;
            firstBuff.Write(buff);

            if (firstBuff.Has<RemoveBuffOnGameplayEvent>())
            {
                firstBuff.Remove<RemoveBuffOnGameplayEvent>();
            }
            if (firstBuff.Has<RemoveBuffOnGameplayEventEntry>())
            {
                firstBuff.Remove<RemoveBuffOnGameplayEventEntry>();
            }
            if (firstBuff.Has<CreateGameplayEventsOnSpawn>())
            {
                firstBuff.Remove<CreateGameplayEventsOnSpawn>();
            }
            if (!firstBuff.Has<Buff_Persists_Through_Death>())
            {
                firstBuff.Add<Buff_Persists_Through_Death>();
            }
            if (firstBuff.Has<LifeTime>())
            {
                LifeTime lifeTime = firstBuff.Read<LifeTime>();
                lifeTime.Duration = -1;
                lifeTime.EndAction = LifeTimeEndAction.None;
                firstBuff.Write(lifeTime);
            }
            if (Core.DataStructures.PlayerClasses.TryGetValue(ctx.Event.User.PlatformId, out var classes) && classes.Keys.Count > 0) // so basically if prestiged already and at the level threshold again, handle the buff matching the index and scale for prestige
            {
                PlayerClasses playerClass = classes.Keys.FirstOrDefault();
                Buff_ApplyBuffOnDamageTypeDealt_DataShared onHitBuff = firstBuff.Read<Buff_ApplyBuffOnDamageTypeDealt_DataShared>();
                onHitBuff.ProcBuff = ClassApplyBuffOnDamageDealtMap[playerClass];
                onHitBuff.ProcChance = 1;
                firstBuff.Write(onHitBuff);

                Core.Log.LogInfo($"Applied {onHitBuff.ProcBuff.GetPrefabName()} to class buff, removing uneeded components...");

                if (firstBuff.Has<Buff_EmpowerDamageDealtByType_DataShared>())
                {
                    firstBuff.Remove<Buff_EmpowerDamageDealtByType_DataShared>();
                }
                if (firstBuff.Has<ModifyMovementSpeedBuff>())
                {
                    firstBuff.Remove<ModifyMovementSpeedBuff>();
                }
                if (firstBuff.Has<CreateGameplayEventOnBuffReapply>())
                {
                    firstBuff.Remove<CreateGameplayEventOnBuffReapply>();
                }
                if (firstBuff.Has<AdjustLifetimeOnGameplayEvent>())
                {
                    firstBuff.Remove<AdjustLifetimeOnGameplayEvent>();
                }
                if (firstBuff.Has<ApplyBuffOnGameplayEvent>())
                {
                    firstBuff.Remove<CreateGameplayEventsOnSpawn>();
                }
                if (firstBuff.Has<SpellModSetComponent>())
                {
                    firstBuff.Remove<SpellModSetComponent>();
                }
                if (firstBuff.Has<ApplyBuffOnGameplayEvent>())
                {
                    firstBuff.Remove<ApplyBuffOnGameplayEvent>();
                }
                if (firstBuff.Has<SpellModArithmetic>())
                {
                    firstBuff.Remove<SpellModArithmetic>();
                }

                       
            }

        }
                // each class gets an applyBuffOnDamageTypeDealt effect? like BloodKnight gets one that has a chance to proc leech, that I could somewhat safely scale with prestige level easily       
    }

    
    */
}