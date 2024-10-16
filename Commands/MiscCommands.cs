using Bloodcraft.Services;
using Bloodcraft.Utilities;
using Epic.OnlineServices.Achievements;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Sequencer;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;
using static Bloodcraft.Services.DataService.PlayerDictionaries;
using static Bloodcraft.Services.PlayerService;
using static VCF.Core.Basics.RoleCommands;
using User = ProjectM.Network.User;

namespace Bloodcraft.Commands;
internal static class MiscCommands
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static CombatMusicSystem_Server CombatMusicSystemServer => SystemService.CombatMusicSystem_Server;
    static ClaimAchievementSystem ClaimAchievementSystem => SystemService.ClaimAchievementSystem;
    static EntityCommandBufferSystem EntityCommandBufferSystem => SystemService.EntityCommandBufferSystem;

    static readonly bool Classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;

    static readonly ComponentType[] DisabledFamiliarComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<Follower>()),
        ComponentType.ReadOnly(Il2CppType.Of<TeamReference>()),
        ComponentType.ReadOnly(Il2CppType.Of<DropTableBuffer>())
    ];

    static readonly ComponentType[] SpawnSequenceComponent =
    [
        ComponentType.ReadOnly(Il2CppType.Of<SpawnSequenceForEntity>()),
    ];

    static readonly PrefabGUID CombatBuff = new(581443919);
    static readonly PrefabGUID NetworkedSequence = new(651179295);

    public static readonly Dictionary<PrefabGUID, int> KitPrefabs = [];

    [Command(name: "reminders", adminOnly: false, usage: ".remindme", description: "Toggles general reminders for various mod features.")]
    public static void LogExperienceCommand(ChatCommandContext ctx)
    {
        var SteamID = ctx.Event.User.PlatformId;

        PlayerUtilities.TogglePlayerBool(SteamID, "Reminders");
        LocalizationService.HandleReply(ctx, $"Reminders {(PlayerUtilities.GetPlayerBool(SteamID, "Reminders") ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
    }

    [Command(name: "sct", adminOnly: false, usage: ".sct", description: "Toggles scrolling text.")]
    public static void ToggleScrollingText(ChatCommandContext ctx)
    {
        var SteamID = ctx.Event.User.PlatformId;

        PlayerUtilities.TogglePlayerBool(SteamID, "ScrollingText");
        LocalizationService.HandleReply(ctx, $"ScrollingText {(PlayerUtilities.GetPlayerBool(SteamID, "ScrollingText") ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
    }

    [Command(name: "starterkit", shortHand: "kitme", adminOnly: false, usage: ".kitme", description: "Provides starting kit.")]
    public static void KitMe(ChatCommandContext ctx)
    {
        if (!ConfigService.StarterKit)
        {
            LocalizationService.HandleReply(ctx, "Starter kit is not enabled.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;

        if (PlayerUtilities.GetPlayerBool(steamId, "Kit")) // if true give kit, if not no
        {
            PlayerUtilities.SetPlayerBool(steamId, "Kit", false);
            Entity character = ctx.Event.SenderCharacterEntity;

            foreach (var item in KitPrefabs)
            {
                ServerGameManager.TryAddInventoryItem(character, item.Key, item.Value);
            }

            string kitItems = KitPrefabs.Select(x => $"<color=#ffd9eb>{x.Key.GetPrefabName()}</color>")
                                        .Aggregate((x, y) => $"{x}, <color=#ffd9eb>{y}</color>");
            LocalizationService.HandleReply(ctx, $"You've received a starting kit with {kitItems}!");
        }
        else
        {
            ctx.Reply("You've already received your starting kit.");
        }
    }

    [Command(name: "prepareforthehunt", shortHand: "prepare", adminOnly: false, usage: ".prepare", description: "Completes GettingReadyForTheHunt if not already completed.")]
    public static void QuickStartCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.LevelingSystem)
        {
            LocalizationService.HandleReply(ctx, "Leveling is not enabled.");
            return;
        }

        EntityCommandBuffer entityCommandBuffer = EntityCommandBufferSystem.CreateCommandBuffer();
        PrefabGUID achievementPrefabGUID = new(560247139); // Journal_GettingReadyForTheHunt

        Entity userEntity = ctx.Event.SenderUserEntity;
        Entity characterEntity = ctx.Event.SenderCharacterEntity;
        Entity achievementOwnerEntity = userEntity.Read<AchievementOwner>().Entity._Entity;

        ClaimAchievementSystem.CompleteAchievement(entityCommandBuffer, achievementPrefabGUID, userEntity, characterEntity, achievementOwnerEntity, false, true);
        LocalizationService.HandleReply(ctx, "You are now prepared for the hunt!");
    }

    [Command(name: "lockspells", shortHand: "locksp", adminOnly: false, usage: ".locksp", description: "Locks in the next spells equipped to use in your unarmed slots.")]
    public static void LockPlayerSpells(ChatCommandContext ctx)
    {
        if (!ConfigService.UnarmedSlots)
        {
            LocalizationService.HandleReply(ctx, "Extra spell slots for unarmed are not enabled.");
            return;
        }

        User user = ctx.Event.User;
        ulong SteamID = user.PlatformId;
        PlayerUtilities.
                TogglePlayerBool(SteamID, "SpellLock");
        if (PlayerUtilities.GetPlayerBool(SteamID, "SpellLock"))
        {
            LocalizationService.HandleReply(ctx, "Change spells to the ones you want in your unarmed slots. When done, toggle this again.");
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Spells set.");
        }
    }

    [Command(name: "lockshift", shortHand: "shift", adminOnly: false, usage: ".shift", description: "Toggle shift spell.")]
    public static void ShiftPlayerSpells(ChatCommandContext ctx)
    {
        if (!Classes)
        {
            LocalizationService.HandleReply(ctx, "Classes are not enabled and spells can't be set to shift.");
            return;
        }

        if (!ConfigService.ShiftSlot)
        {
            LocalizationService.HandleReply(ctx, "Shift slots are not enabled.");
            return;
        }

        User user = ctx.Event.User;
        ulong steamId = user.PlatformId;

        Entity character = ctx.Event.SenderCharacterEntity;
        if (!InventoryUtilities.TryGetInventoryEntity(EntityManager, character, out Entity inventoryEntity) || InventoryUtilities.IsInventoryFull(EntityManager, inventoryEntity))
        {
            LocalizationService.HandleReply(ctx, "Can't change or active class spells when inventory is full, need at least one space to safely handle jewels when switching.");
            return;
        }

        PlayerUtilities.TogglePlayerBool(steamId, "ShiftLock");
        if (PlayerUtilities.GetPlayerBool(steamId, "ShiftLock"))
        {
            if (steamId.TryGetPlayerSpells(out var spellsData))
            {
                PrefabGUID spellPrefabGUID = new(spellsData.ClassSpell);

                if (spellPrefabGUID.HasValue())
                {
                    ClassUtilities.UpdateShift(ctx, ctx.Event.SenderCharacterEntity, spellPrefabGUID);
                }
            }
            LocalizationService.HandleReply(ctx, "Shift spell <color=green>enabled</color>.");
        }
        else
        {
            ClassUtilities.RemoveShift(ctx.Event.SenderCharacterEntity);
            LocalizationService.HandleReply(ctx, "Shift spell <color=red>disabled</color>.");
        }
    }

    [Command(name: "userstats", adminOnly: false, usage: ".userstats", description: "Shows neat information about the player.")]
    public static void GetUserStats(ChatCommandContext ctx)
    {
        Entity userEntity = ctx.Event.SenderUserEntity;

        UserStats userStats = userEntity.Read<UserStats>();
        int VBloodKills = userStats.VBloodKills;
        int UnitKills = userStats.UnitKills;
        int Deaths = userStats.Deaths;
        float OnlineTime = userStats.OnlineTime;
        OnlineTime = (int)OnlineTime / 3600;
        float DistanceTraveled = userStats.DistanceTravelled;
        DistanceTraveled = (int)DistanceTraveled / 1000;
        float LitresBloodConsumed = userStats.LitresBloodConsumed;
        LitresBloodConsumed = (int)LitresBloodConsumed;

        LocalizationService.HandleReply(ctx, $"<color=white>VBloods Slain</color>: <color=#FF5733>{VBloodKills}</color> | <color=white>Units Killed</color>: <color=#FFD700>{UnitKills}</color> | <color=white>Deaths</color>: <color=#808080>{Deaths}</color> | <color=white>Time Online</color>: <color=#1E90FF>{OnlineTime}</color>hr | <color=white>Distance Traveled</color>: <color=#32CD32>{DistanceTraveled}</color>kf | <color=white>Blood Consumed</color>: <color=red>{LitresBloodConsumed}</color>L");
    }

    [Command(name: "silence", adminOnly: false, usage: ".silence", description: "Resets music for player.")]
    public static void ResetMusicCommand(ChatCommandContext ctx)
    {
        Entity character = ctx.Event.SenderCharacterEntity;

        if (ServerGameManager.HasBuff(character, CombatBuff.ToIdentifier()))
        {
            LocalizationService.HandleReply(ctx, "This command should only be used as required and certainly not while in combat.");
            return;
        }

        CombatMusicListener_Shared combatMusicListener_Shared = character.Read<CombatMusicListener_Shared>();
        combatMusicListener_Shared.UnitPrefabGuid = new PrefabGUID(0);
        character.Write(combatMusicListener_Shared);

        CombatMusicSystemServer.OnUpdate();
        ctx.Reply($"Combat music cleared~");
    }

    [Command(name: "cleanupfams", adminOnly: true, usage: ".cleanupfams", description: "Removes disabled, invisible familiars on the map preventing building.")]
    public static void CleanUpFams(ChatCommandContext ctx)
    {
        EntityQuery familiarsQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = DisabledFamiliarComponents,
            Options = EntityQueryOptions.IncludeDisabled
        });

        int counter = 0;
        try
        {
            Dictionary<ulong, (Entity Familiar, int FamKey)> FamiliarActives = new(familiarActives);
            List<Entity> dismissedFamiliars = familiarActives.Values.Select(x => x.Familiar).ToList();

            IEnumerable<Entity> disabledFamiliars = EntityUtilities.GetEntitiesEnumerable(familiarsQuery); // need to filter for active/dismissed familiars and not destroy them
            foreach (Entity entity in disabledFamiliars)
            {
                if (dismissedFamiliars.Contains(entity)) continue;
                else
                {
                    if (entity.GetTeamEntity().Has<UserTeam>() && entity.ReadBuffer<DropTableBuffer>()[0].DropTrigger.Equals(DropTriggerType.OnSalvageDestroy))
                    {
                        if (entity.Has<Disabled>())
                        {
                            entity.Remove<Disabled>();
                            DestroyUtility.Destroy(EntityManager, entity);
                            counter++;
                        }
                        else if (entity.Has<DisabledDueToNoPlayersInRange>())
                        {
                            DestroyUtility.Destroy(EntityManager, entity);
                            counter++;
                        }
                    }
                }
            }
        }
        finally
        {
            familiarsQuery.Dispose();
            LocalizationService.HandleReply(ctx, $"Destroyed <color=white>{counter}</color> disabled familiars...");
        }

        EntityQuery networkedSequencesQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = SpawnSequenceComponent,
            Options = EntityQueryOptions.IncludeDisabled
        });

        counter = 0;
        try
        {
            IEnumerable<Entity> networkedSequences = EntityUtilities.GetEntitiesEnumerable(networkedSequencesQuery);

            foreach (Entity entity in networkedSequences)
            {
                if (entity.TryGetComponent(out PrefabGUID prefab) && prefab.Equals(NetworkedSequence))
                {
                    SpawnSequenceForEntity spawnSequenceForEntity = entity.Read<SpawnSequenceForEntity>();

                    Entity target = spawnSequenceForEntity.Target.GetEntityOnServer();
                    Entity secondaryTarget = spawnSequenceForEntity.SecondaryTarget.GetEntityOnServer();

                    if (secondaryTarget.TryGetComponent(out PrefabGUID secondaryTargetPrefab) && secondaryTarget.Has<BlockFeedBuff>())
                    {
                        DestroyUtility.Destroy(EntityManager, secondaryTarget, DestroyDebugReason.None);
                        counter++;
                    }

                    DestroyUtility.Destroy(EntityManager, entity, DestroyDebugReason.None);
                }
            }
        }
        finally
        {
            networkedSequencesQuery.Dispose();
            LocalizationService.HandleReply(ctx, $"Destroyed <color=white>{counter}</color> disabled summons...");
        }

        Dictionary<string, PlayerInfo> playerCache = new(PlayerCache);
        counter = 0;

        foreach (var keyValuePair in playerCache)
        {
            Entity playerCharacter = keyValuePair.Value.CharEntity;
            User user = keyValuePair.Value.User;

            if (!user.IsConnected && ServerGameManager.TryGetBuffer<FollowerBuffer>(playerCharacter, out var followerBuffer) && ServerGameManager.TryGetBuffer<MinionBuffer>(playerCharacter, out var minionBuffer))
            {
                foreach (FollowerBuffer follower in followerBuffer)
                {
                    Entity followerEntity = follower.Entity.GetEntityOnServer();

                    if (followerEntity.Exists())
                    {
                        DestroyUtility.Destroy(EntityManager, followerEntity);
                        counter++;
                    }
                }

                followerBuffer.Clear();

                foreach (MinionBuffer minion in minionBuffer)
                {
                    Entity minionEntity = minion.Entity;

                    if (minionEntity.Exists())
                    {
                        DestroyUtility.Destroy(EntityManager, minionEntity);
                        counter++;
                    }
                }

                minionBuffer.Clear();
            }
        }

        LocalizationService.HandleReply(ctx, $"Destroyed <color=white>{counter}</color> entities found in player FollowerBuffers and MinionBuffers...");
    }

    //[Command(name: "switcheroo", adminOnly: true, usage: ".switch [OriginalPlayer] [NewPlayer]", description: "Swaps the steamIDs of two players for testing.")] this is just swapplayers without kicking people to use their mod data, ty Odjit <3
    public static void SwitchPlayers(ChatCommandContext ctx, string originalPlayer, string newPlayer)
    {
        if (originalPlayer.TryGetPlayerInfo(out PlayerInfo originalPlayerInfo) && newPlayer.TryGetPlayerInfo(out PlayerInfo newPlayerInfo))
        {
            Entity originalUserEntity = originalPlayerInfo.UserEntity;
            Entity newUserEntity = newPlayerInfo.UserEntity;

            User originalUser = originalUserEntity.Read<User>();
            User newUser = newUserEntity.Read<User>();

            (originalUser.PlatformId, newUser.PlatformId) = (newUser.PlatformId, originalUser.PlatformId);

            originalUserEntity.Write(originalUser);
            newUserEntity.Write(newUser);

            ctx.Reply($"Switched steamIds for {originalPlayerInfo.User.CharacterName} with {newPlayerInfo.User.CharacterName}!");
        }
    }
}
