using Bloodcraft.Patches;
using Bloodcraft.Services;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Utilities;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Sequencer;
using ProjectM.Shared;
using Steamworks;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;
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

    [Command(name: "reminders", shortHand: "remindme", adminOnly: false, usage: ".remindme", description: "Toggles general reminders for various mod features.")]
    public static void LogExperienceCommand(ChatCommandContext ctx)
    {
        var SteamID = ctx.Event.User.PlatformId;

        Misc.TogglePlayerBool(SteamID, "Reminders");
        LocalizationService.HandleReply(ctx, $"Reminders {(Misc.GetPlayerBool(SteamID, "Reminders") ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
    }

    [Command(name: "sct", adminOnly: false, usage: ".sct", description: "Toggles scrolling text.")]
    public static void ToggleScrollingText(ChatCommandContext ctx)
    {
        var SteamID = ctx.Event.User.PlatformId;

        Misc.TogglePlayerBool(SteamID, "ScrollingText");
        LocalizationService.HandleReply(ctx, $"ScrollingText {(Misc.GetPlayerBool(SteamID, "ScrollingText") ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
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

        if (!Misc.GetPlayerBool(steamId, "Kit")) // if true give kit, if not no
        {
            Misc.SetPlayerBool(steamId, "Kit", true);
            Entity character = ctx.Event.SenderCharacterEntity;

            foreach (var item in KitPrefabs)
            {
                ServerGameManager.TryAddInventoryItem(character, item.Key, item.Value);
            }

            List<string> kitItems = KitPrefabs.Select(x => $"<color=white>{x.Key.GetPrefabName()}</color>").ToList();

            LocalizationService.HandleReply(ctx, $"You've received a starting kit with:");

            const int maxPerMessage = 6;
            for (int i = 0; i < kitItems.Count; i += maxPerMessage)
            {
                var batch = kitItems.Skip(i).Take(maxPerMessage);
                string items = string.Join(", ", batch);

                LocalizationService.HandleReply(ctx, $"{items}");
            }
        }
        else
        {
            ctx.Reply("You've already received your starting kit!");
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
        Misc.TogglePlayerBool(SteamID, "SpellLock");
            
        if (Misc.GetPlayerBool(SteamID, "SpellLock"))
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

        Misc.TogglePlayerBool(steamId, "ShiftLock");
        if (Misc.GetPlayerBool(steamId, "ShiftLock"))
        {
            if (steamId.TryGetPlayerSpells(out var spellsData))
            {
                PrefabGUID spellPrefabGUID = new(spellsData.ClassSpell);

                if (spellPrefabGUID.HasValue())
                {
                    Utilities.Classes.UpdateShift(ctx, ctx.Event.SenderCharacterEntity, spellPrefabGUID);
                    //EclipseService.SendClientAbilityData(ctx.Event.SenderCharacterEntity);
                }
            }

            LocalizationService.HandleReply(ctx, "Shift spell <color=green>enabled</color>.");
        }
        else
        {
            Utilities.Classes.RemoveShift(ctx.Event.SenderCharacterEntity);
            //EclipseService.SendClientAbilityData(ctx.Event.SenderCharacterEntity);

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

    [Command(name: "exoform", adminOnly: false, usage: ".exoform", description: "Toggles taunting to enter exo form.")]
    public static void ToggleExoFormEmote(ChatCommandContext ctx)
    {
        if (!ConfigService.ExoPrestiging)
        {
            ctx.Reply("Exo prestiging is not enabled.");
        }

        ulong steamId = ctx.Event.User.PlatformId;

        if (steamId.TryGetPlayerPrestiges(out var prestiges) && prestiges.TryGetValue(PrestigeType.Exo, out int exoPrestiges) && exoPrestiges > 0)
        {
            if (!Misc.ConsumedDracula(ctx.Event.SenderUserEntity))
            {
                ctx.Reply("You must consume Dracula's essence before manifesting this power...");
                return;
            }

            Misc.TogglePlayerBool(steamId, "ExoForm");
            ctx.Reply($"Exo form emote action (<color=white>taunt</color>) {(Misc.GetPlayerBool(steamId, "ExoForm") ? "<color=green>enabled</color>, the Immortal King's formidable powers are now yours..." : "<color=red>disabled</color>...")}");
        }
        else
        {
            ctx.Reply("You are not yet worthy...");
        }
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
            Dictionary<ulong, (Entity Familiar, int FamKey)> FamiliarActives = new(DataService.PlayerDictionaries.familiarActives);
            List<Entity> dismissedFamiliars = FamiliarActives.Values.Select(x => x.Familiar).ToList();

            IEnumerable<Entity> disabledFamiliars = Queries.GetEntitiesEnumerable(familiarsQuery); // need to filter for active/dismissed familiars and not destroy them
            foreach (Entity entity in disabledFamiliars)
            {
                if (dismissedFamiliars.Contains(entity)) continue;
                else
                {
                    if (entity.TryGetTeamEntity(out Entity teamEntity) && teamEntity.Has<UserTeam>() && entity.TryGetBuffer<DropTableBuffer>(out var buffer) && buffer[0].DropTrigger.Equals(DropTriggerType.OnSalvageDestroy))
                    {
                        if (entity.Has<Disabled>())
                        {
                            entity.Remove<Disabled>();
                            if (entity.Has<DisableWhenNoPlayersInRange>()) entity.Remove<DisableWhenNoPlayersInRange>();
                            if (entity.Has<DisabledDueToNoPlayersInRange>()) entity.Remove<DisabledDueToNoPlayersInRange>();

                            EntityManager.DestroyEntity(entity);
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
            IEnumerable<Entity> networkedSequences = Queries.GetEntitiesEnumerable(networkedSequencesQuery);

            foreach (Entity entity in networkedSequences)
            {
                if (entity.TryGetComponent(out PrefabGUID prefab) && prefab.Equals(NetworkedSequence))
                {
                    SpawnSequenceForEntity spawnSequenceForEntity = entity.Read<SpawnSequenceForEntity>();

                    Entity target = spawnSequenceForEntity.Target.GetEntityOnServer();
                    Entity secondaryTarget = spawnSequenceForEntity.SecondaryTarget.GetEntityOnServer();

                    if (secondaryTarget.TryGetComponent(out PrefabGUID secondaryTargetPrefab) && secondaryTarget.Has<BlockFeedBuff>())
                    {
                        if (secondaryTarget.Has<Disabled>()) secondaryTarget.Remove<Disabled>();
                        if (secondaryTarget.Has<DisableWhenNoPlayersInRange>()) secondaryTarget.Remove<DisableWhenNoPlayersInRange>();
                        if (secondaryTarget.Has<DisabledDueToNoPlayersInRange>()) secondaryTarget.Remove<DisabledDueToNoPlayersInRange>();

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
                        if (followerEntity.Has<Disabled>()) followerEntity.Remove<Disabled>();
                        if (followerEntity.Has<DisableWhenNoPlayersInRange>()) followerEntity.Remove<DisableWhenNoPlayersInRange>();
                        if (followerEntity.Has<DisabledDueToNoPlayersInRange>()) followerEntity.Remove<DisabledDueToNoPlayersInRange>();

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
                        if (minionEntity.Has<Disabled>()) minionEntity.Remove<Disabled>();
                        if (minionEntity.Has<DisableWhenNoPlayersInRange>()) minionEntity.Remove<DisableWhenNoPlayersInRange>();
                        if (minionEntity.Has<DisabledDueToNoPlayersInRange>()) minionEntity.Remove<DisabledDueToNoPlayersInRange>();

                        DestroyUtility.Destroy(EntityManager, minionEntity);
                        counter++;
                    }
                }

                minionBuffer.Clear();
            }
        }

        LocalizationService.HandleReply(ctx, $"Destroyed <color=white>{counter}</color> entities found in player FollowerBuffers and MinionBuffers...");
    }
}