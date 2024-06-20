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
using static Bloodcraft.Systems.Experience.LevelingSystem.AllianceUtilities;


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

    [Command(name: "quickStart", shortHand: "start", adminOnly: false, usage: ".start", description: "Completes GettingReadyForTheHunt if not already completed.")]
    public static void QuickStartCommand(ChatCommandContext ctx)
    {
        if (!Leveling)
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
        if (!Leveling)
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
        if (!Leveling)
        {
            HandleReply(ctx, "Leveling is not enabled.");
            return;
        }
        ulong steamId = ctx.Event.User.PlatformId;
        if (Core.DataStructures.PlayerExperience.TryGetValue(steamId, out var levelKvp))
        {
            int level = levelKvp.Key;
            int progress = (int)(levelKvp.Value - LevelingSystem.ConvertLevelToXp(level));
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
        if (!Leveling)
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

        if (level < 0 || level > MaxPlayerLevel)
        {
            HandleReply(ctx, $"Level must be between 0 and {MaxPlayerLevel}");
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
        if (!SoftSynergies && !HardSynergies)
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
        if (!SoftSynergies && !HardSynergies)
        {
            HandleReply(ctx, "Classes are not enabled.");
            return;
        }
        if (!ShiftSlot)
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
        if (!SoftSynergies && !HardSynergies)
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
        if (!SoftSynergies && !HardSynergies)
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
        if (!SoftSynergies && !HardSynergies)
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

            int step = MaxPlayerLevel / perks.Count;

            string replyMessage = string.Join(", ", perks.Select((perk, index) =>
            {
                int level = (index + 1) * step;
                string prefab = new PrefabGUID(perk).LookupName();
                int prefabIndex = prefab.IndexOf("Prefab");
                if (prefabIndex != -1)
                {
                    prefab = prefab[..prefabIndex].TrimEnd();
                }
                return $"<color=white>{prefab}</color> at level <color=yellow>{level}</color>";
            }));
            HandleReply(ctx, $"{playerClass} buffs: {replyMessage}");
        }
        else
        {
            HandleReply(ctx, "You haven't chosen a class yet.");
        }
    }

    [Command(name: "listClassSpells", shortHand: "lcs", adminOnly: false, usage: ".lcs", description: "Shows perks that can be gained from class.")]
    public static void ListClassSpells(ChatCommandContext ctx)
    {
        if (!SoftSynergies && !HardSynergies)
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
        if (!Prestige)
        {
            HandleReply(ctx, "Prestiging is not enabled.");
            return;
        }

        if (!PrestigeSystem.TryParsePrestigeType(prestigeType, out var parsedPrestigeType))
        {
            HandleReply(ctx, "Invalid prestige, use .lpp to see options.");
            return;
        }

        if ((SoftSynergies || HardSynergies) &&
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
        if (!Prestige)
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
        if (!Prestige)
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
        if (!Prestige)
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
        if (!Prestige)
        {
            HandleReply(ctx, "Prestiging is not enabled.");
            return;
        }
        string prestigeTypes = string.Join(", ", Enum.GetNames(typeof(PrestigeSystem.PrestigeType)));
        HandleReply(ctx, $"Available Prestiges: <color=#90EE90>{prestigeTypes}</color>");
    }

    [Command(name: "toggleAllianceInvites", shortHand: "invites", adminOnly: false, usage: ".invites", description: "Toggles being able to be invited to an alliance. Allowed in raids of allied players and share exp if applicable.")]
    public static void ToggleAllianceInvitesCommand(ChatCommandContext ctx)
    {
        if (!PlayerAlliances)
        {
            HandleReply(ctx, "Alliances are not enabled.");
            return;
        }

        ulong SteamID = ctx.Event.User.PlatformId;
        Entity ownerClanEntity = ctx.Event.User.ClanEntity._Entity;
        string name = ctx.Event.User.CharacterName.Value;

        if (ClanAlliances && ownerClanEntity.Equals(Entity.Null) || !Core.EntityManager.Exists(ownerClanEntity))
        {
            HandleReply(ctx, "You must be the leader of a clan to toggle alliance invites.");
            return;
        }
        else if (ClanAlliances)
        {
            Entity userEntity = ctx.Event.SenderUserEntity;
            if (userEntity.TryGetComponent(out ClanRole clanRole) && !clanRole.Value.Equals(ClanRoleEnum.Leader))
            {
                HandleReply(ctx, "You must be the leader of a clan to toggle alliance invites.");
                return;
            }
        }

        if (Core.DataStructures.PlayerAlliances.Any(kvp => kvp.Value.Contains(name)))
        {
            HandleReply(ctx, "You are already in an alliance. Leave or disband before enabling invites.");
            return;
        }

        if (Core.DataStructures.PlayerBools.TryGetValue(SteamID, out var bools))
        {
            bools["Grouping"] = !bools["Grouping"];
        }
        Core.DataStructures.SavePlayerBools();
        HandleReply(ctx, $"Alliance invites {(bools["Grouping"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
    }

    [Command(name: "allianceAdd", shortHand: "aa", adminOnly: false, usage: ".aa [Player/Clan]", description: "Adds player/clan to alliance if invites are toggled (if clan based owner of clan must toggle).")]
    public static void AllianceAddCommand(ChatCommandContext ctx, string name)
    {  
        if (!PlayerAlliances)
        {
            HandleReply(ctx, "Alliances are not enabled.");
            return;
        }

        ulong ownerId = ctx.Event.User.PlatformId;
        Entity ownerClanEntity = ctx.Event.User.ClanEntity._Entity;

        if (ClanAlliances)
        {
            if (CheckClanLeadership(ctx, ownerClanEntity))
            {
                HandleReply(ctx, "You must be the leader of a clan to form an alliance.");
                return;
            }

            HandleClanAlliance(ctx, ownerId, name);
        }
        else
        {
            HandlePlayerAlliance(ctx, ownerId, name);
        }
        /*
        if (!PlayerAlliances)
        {
            HandleReply(ctx, "Alliances are not enabled.");
            return;
        }

        ulong ownerId = ctx.Event.User.PlatformId;
        Entity ownerClanEntity = ctx.Event.User.ClanEntity._Entity;

        if (ClanAlliances && ownerClanEntity.Equals(Entity.Null))
        {
            HandleReply(ctx, "You must be the leader of a clan to form an alliance.");
            return;
        }
        else if (ClanAlliances)
        {
            Entity userEntity = ctx.Event.SenderUserEntity;
            if (userEntity.TryGetComponent(out ClanRole clanRole) && !clanRole.Value.Equals(ClanRoleEnum.Leader))
            {
                HandleReply(ctx, "You must be the leader of a clan to form an alliance.");
                return;
            }
        }    

        if (ClanAlliances)
        {
            //ClanMemberStatus query for entities with ClanMemberStatus then match name to clan name
            if (!Core.DataStructures.PlayerAlliances.ContainsKey(ownerId)) // check if inviter has a group, make one if not
            {
                Core.DataStructures.PlayerAlliances[ownerId] = [];
            }

            HashSet<string> alliance = Core.DataStructures.PlayerAlliances[ownerId];
            HashSet<string> members = [];
            Entity clanEntity = GetClanByName(name);

            if (clanEntity.Equals(Entity.Null))
            {
                HandleReply(ctx, "Clan/leader not found...");
                return;
            }

            var clanBuffer = clanEntity.ReadBuffer<ClanMemberStatus>();
            int leaderIndex = -1;

            for (int i = 0; i < clanBuffer.Length; i++) // find leader, check invite toggle
            {
                if (clanBuffer[i].ClanRole.Equals(ClanRoleEnum.Leader))
                {
                    leaderIndex = i;
                    break;
                }
            }

            if (leaderIndex == -1)
            {
                HandleReply(ctx, "Couldn't find clan leader to verify consent.");
                return;
            }

            var userBuffer = clanEntity.ReadBuffer<SyncToUserBuffer>();
            for (int i = 0; i < userBuffer.Length; i++) // add clan members
            {
                var users = userBuffer[i];
                User user = users.UserEntity.Read<User>();
                if (i == leaderIndex && Core.DataStructures.PlayerBools.TryGetValue(user.PlatformId, out var bools) && !bools["Grouping"]) // check for invites here on leader
                {
                    HandleReply(ctx, "Clan leader does not have alliances invites enabled.");
                    return;
                }
                members.Add(user.CharacterName.Value);
            }
            
            if (members.Count > 0 && alliance.Count + members.Count < Plugin.MaxAllianceSize.Value)
            {
                string membersAdded = string.Join(", ", members.Select(member => $"<color=green>{member}</color>"));
                alliance.UnionWith(members);
                HandleReply(ctx, $"{membersAdded} were added to the alliance.");
                Core.DataStructures.SavePlayerAlliances();
            }
            else if (members.Count == 0)
            {
                HandleReply(ctx, "Couldn't find any clan members to add.");
            }
            else
            {
                HandleReply(ctx, "Alliance would exceed max size by adding found clan members.");
            }
        }
        else
        {
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
                    alliance.Add(playerName); // would need to add the playerTeam TeamReference entity for every player in the alliance to the TeamAllies buffer of... every other TeamAllies buffer on every TeamReference entity on every other player in the alliance, oof
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
        */
    }

    [Command(name: "allianceRemove", shortHand: "ar", adminOnly: false, usage: ".ar [Player/Clan]", description: "Removes player or clan from alliance.")]
    public static void AllianceRemoveCommand(ChatCommandContext ctx, string name)
    {
        if (!PlayerAlliances)
        {
            HandleReply(ctx, "Alliances are not enabled.");
            return;
        }

        ulong ownerId = ctx.Event.User.PlatformId;
        Entity ownerClanEntity = ctx.Event.User.ClanEntity._Entity;

        if (ClanAlliances && CheckClanLeadership(ctx, ownerClanEntity))
        {
            HandleReply(ctx, "You must be the leader of a clan to remove clans from an alliance.");
            return;
        }

        if (!Core.DataStructures.PlayerAlliances.ContainsKey(ownerId))
        {
            HandleReply(ctx, "You don't have an alliance.");
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
        /*
        if (ClanAlliances) // find members of alliance with matching clan name and remove
        {
            List<string> removed = [];
            
            Entity clanEntity = GetClanByName(name);
            if (clanEntity.Equals(Entity.Null))
            {
                HandleReply(ctx, "Clan/leader not found...");
                return;
            }
            foreach (string memberName in alliance)
            {
                string playerKey = playerCache.Keys.FirstOrDefault(key => key.Equals(memberName, StringComparison.OrdinalIgnoreCase));
                if (playerCache.TryGetValue(playerKey, out var player))
                {
                    Entity playerClanEntity = player.Read<User>().ClanEntity._Entity;
                    ClanTeam clanTeam = playerClanEntity.Read<ClanTeam>();
                    if (clanTeam.Name.Value.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        alliance.Remove(memberName);
                        removed.Add(memberName);
                    }
                }
            }
            string replyMessage = removed.Count > 0 ? string.Join(", ", removed.Select(member => $"<color=green>{member}</color>")) : "No members matching clan name found to remove.";
            if (removed.Count > 0) replyMessage += " removed from alliance.";
            HandleReply(ctx, replyMessage);
            Core.DataStructures.SavePlayerAlliances();
            return;
        }
        else
        {
            if (alliance.FirstOrDefault(n => n.Equals(name, StringComparison.OrdinalIgnoreCase)) != null)
            {
                alliance.Remove(name);
                Core.DataStructures.SavePlayerAlliances();
                HandleReply(ctx, $"<color=green>{char.ToUpper(name[0]) + name[1..].ToLower()}</color> removed from alliance.");
            }
            else
            {
                HandleReply(ctx, $"<color=green>{char.ToUpper(name[0]) + name[1..].ToLower()}</color> not found in alliance.");
            }
        }
        */
    }

    [Command(name: "listAllianceMembers", shortHand: "lam", adminOnly: false, usage: ".lam [Player]", description: "Lists alliance members of your alliance or the alliance you are in or the members in the alliance of the player entered if found.")]
    public static void AllianceMembersCommand(ChatCommandContext ctx, string name = "")
    {
        if (!PlayerAlliances)
        {
            HandleReply(ctx, "Alliances are not enabled.");
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
        /*
        if (string.IsNullOrEmpty(name)) // get personal alliance members if no name entered
        {
            ulong ownerId = ctx.Event.User.PlatformId;
            string playerName = ctx.Event.User.CharacterName.Value;
            HashSet<string> members = playerAlliances.ContainsKey(ownerId) ? playerAlliances[ownerId] : playerAlliances.Where(groupEntry => groupEntry.Value.Contains(playerName)).SelectMany(groupEntry => groupEntry.Value).ToHashSet();
            string replyMessage = members.Count > 0 ? string.Join(", ", members.Select(member => $"<color=green>{member}</color>")) : "No members in alliance.";
            HandleReply(ctx, replyMessage);
        }
        else
        {
            string playerKey = playerCache.Keys.FirstOrDefault(key => key.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(playerKey) && playerCache.TryGetValue(playerKey, out var player))
            {
                ulong steamId = player.Read<User>().PlatformId;
                string playerName = player.Read<User>().CharacterName.Value;
                HashSet<string> members = playerAlliances.ContainsKey(steamId) ? playerAlliances[steamId] : playerAlliances.Where(groupEntry => groupEntry.Value.Contains(playerName)).SelectMany(groupEntry => groupEntry.Value).ToHashSet();
                string replyMessage = members.Count > 0 ? string.Join(", ", members.Select(member => $"<color=green>{member}</color>")) : "No members in alliance.";
                HandleReply(ctx, replyMessage);
            }
            else
            {                
                foreach (var groupEntry in playerAlliances)
                {
                    playerKey = groupEntry.Value.FirstOrDefault(key => key.Equals(name, StringComparison.OrdinalIgnoreCase));
                    if (!string.IsNullOrEmpty(playerKey))
                    {
                        string replyMessage = groupEntry.Value.Count > 0 ? string.Join(", ", groupEntry.Value.Select(member => $"<color=green>{member}</color>")) : "No members in alliance.";
                        HandleReply(ctx, replyMessage);
                        return;
                    }
                }
            }
        }
        */
    }

    [Command(name: "allianceDisband", shortHand: "disband", adminOnly: false, usage: ".disband", description: "Disbands alliance.")]
    public static void DisbandAllianceCommand(ChatCommandContext ctx)
    {
        if (!PlayerAlliances)
        {
            HandleReply(ctx, "Alliances are not enabled.");
            return;
        }

        ulong ownerId = ctx.Event.User.PlatformId;
        Entity ownerClanEntity = ctx.Event.User.ClanEntity._Entity;

        if (ClanAlliances && CheckClanLeadership(ctx, ownerClanEntity))
        {
            HandleReply(ctx, "You must be the leader of your clan to disband the alliance.");
            return;
        }

        if (!Core.DataStructures.PlayerAlliances.ContainsKey(ownerId)) 
        {
            HandleReply(ctx, "You don't have an alliance to disband.");
            return;
        }
       
        Core.DataStructures.PlayerAlliances.Remove(ownerId);
        HandleReply(ctx, "Alliance disbanded.");
        Core.DataStructures.SavePlayerAlliances();   
    }

    [Command(name: "leaveAlliance", shortHand: "leave", adminOnly: false, usage: ".leave", description: "Leaves alliance if in one.")]
    public static void LeaveAllianceCommand(ChatCommandContext ctx)
    {
        if (!PlayerAlliances)
        {
            HandleReply(ctx, "Alliances are not enabled.");
            return;
        }

        ulong ownerId = ctx.Event.User.PlatformId;
        Entity ownerClanEntity = ctx.Event.User.ClanEntity._Entity;
        string playerName = ctx.Event.User.CharacterName.Value;

        if (ClanAlliances && CheckClanLeadership(ctx, ownerClanEntity))
        {
            HandleReply(ctx, "You must be the leader of a clan to leave an alliance.");
            return;
        }

        if (Core.DataStructures.PlayerAlliances.ContainsKey(ownerId))
        {
            HandleReply(ctx, "You can't leave your own alliance. Disband it instead.");
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
                HandleReply(ctx, "Your clan is not in an alliance.");
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
                HandleReply(ctx, "You're not in an alliance.");
            }    
        }
        /*
        if (ClanAlliances && ownerClanEntity.Equals(Entity.Null) || !Core.EntityManager.Exists(ownerClanEntity))
        {
            HandleReply(ctx, "You must be the leader of a clan to remove it from an alliance.");
            return;
        }
        else if (ClanAlliances)
        {
            Entity userEntity = ctx.Event.SenderUserEntity;
            if (userEntity.TryGetComponent(out ClanMemberStatus memberStatus) && !memberStatus.Equals(ClanRoleEnum.Leader))
            {
                HandleReply(ctx, "You must be the leader of a clan to remove it from an alliance.");
                return;
            }
        }


        if (ClanAlliances)
        {
            // find alliance that this clan leader is in, remove matching clan members
            List<string> removed = [];
            var alliance = Core.DataStructures.PlayerAlliances.Values.FirstOrDefault(set => set.Contains(playerName)); // this set has the clan members
            foreach (var member in alliance)
            {
                if (playerCache.TryGetValue(member, out var player))
                {
                    Entity playerClanEntity = player.Read<User>().ClanEntity._Entity;
                    if (playerClanEntity.Equals(ownerClanEntity))
                    {
                        alliance.Remove(member);
                        removed.Add(member);
                    }
                }
            }
            string replyMessage = removed.Count > 0 ? string.Join(", ", removed.Select(member => $"<color=green>{member}</color>")) : "Failed to leave the alliance.";
            if (removed.Count > 0) replyMessage += " removed from alliance.";
            HandleReply(ctx, replyMessage);
            Core.DataStructures.SavePlayerAlliances();
        }
        else
        {
            var alliance = Core.DataStructures.PlayerAlliances.Values.FirstOrDefault(set => set.Contains(playerName));
            if (alliance != null)
            {
                alliance.Remove(playerName);
                HandleReply(ctx, "You have left the alliance.");
                Core.DataStructures.SavePlayerAlliances();
            }
            else
            {
                HandleReply(ctx, "You are not in an alliance.");
            }
        }
        */
    }
}