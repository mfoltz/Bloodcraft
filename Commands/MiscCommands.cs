using Bloodcraft.Services;
using Bloodcraft.Utilities;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Entities;
using Unity.Transforms;
using VampireCommandFramework;
using static Bloodcraft.Services.DataService.PlayerDictionaries;
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
        ComponentType.ReadOnly(Il2CppType.Of<Disabled>()),
        ComponentType.ReadOnly(Il2CppType.Of<TeamReference>()),
        ComponentType.ReadOnly(Il2CppType.Of<DropTableBuffer>())
    ];

    static readonly PrefabGUID combatBuff = new(581443919);
    public static Dictionary<PrefabGUID, int> KitPrefabs = [];

    [Command(name: "reminders", adminOnly: false, usage: ".remindme", description: "Toggles general reminders for various mod features.")]
    public static void LogExperienceCommand(ChatCommandContext ctx)
    {
        var SteamID = ctx.Event.User.PlatformId;
        PlayerUtilities.
                TogglePlayerBool(SteamID, "Reminders");
        LocalizationService.HandleReply(ctx, $"Reminders {(PlayerUtilities.GetPlayerBool(SteamID, "Reminders") ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
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

            LocalizationService.HandleReply(ctx, "You've received a starting kit with blood essence, stone, wood, and bone!");
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

    [Command(name: "lockshift", shortHand: "shift", adminOnly: false, usage: ".shift", description: "Locks in second spell to shift on weapons.")]
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
        ulong SteamID = user.PlatformId;
        PlayerUtilities.
                TogglePlayerBool(SteamID, "ShiftLock");
        if (PlayerUtilities.GetPlayerBool(SteamID, "ShiftLock"))
        {
            LocalizationService.HandleReply(ctx, "Shift spell <color=green>enabled</color>.");
        }
        else
        {
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

        if (ServerGameManager.HasBuff(character, combatBuff.ToIdentifier()))
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

    //[Command(name: "servantfam", adminOnly: true, usage: ".servantfam", description: "Tired of remaking commands to test one thing at a time, just gonna leave this here and comment out in future releases :p")]
    public static void ServantFamTesting(ChatCommandContext ctx)
    {
        Entity character = ctx.Event.SenderCharacterEntity;
        Entity familiar = FamiliarUtilities.FindPlayerFamiliar(character);
        if (!familiar.Exists()) return;

        //FamiliarPatches.PlayerEntities.Enqueue(character);

        EntityCommandBuffer entityCommandBuffer = EntityCommandBufferSystem.CreateCommandBuffer();
        SpawnDebugEvent spawnDebugEvent = new()
        {
            Control = false,
            Position = familiar.Read<Translation>().Value,
            Level = 1,
            Team = SpawnDebugEvent.TeamEnum.Ally,
            PrefabGuid = new(1649578802)
        };

        FromCharacter fromCharacter = new()
        {
            Character = character,
            User = ctx.Event.SenderUserEntity
        };

        Core.SystemService.DebugEventsSystem.SpawnDebugEvent(ctx.Event.User.Index, ref spawnDebugEvent, entityCommandBuffer, ref fromCharacter);
    }

    [Command(name: "cleanupfams", adminOnly: true, usage: ".cleanupfams", description: "Removes disabled, invisible familiars on the map preventing building.")]
    public static void CleanUpFams(ChatCommandContext ctx)
    {
        // BlockFeedBuff, Disabled, TeamReference with UserTeam on entity, and see if name of prefab starts with CHAR?
        EntityQuery familiarsQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = DisabledFamiliarComponents,
            Options = EntityQueryOptions.IncludeDisabled
        });

        Dictionary<ulong, (Entity Familiar, int FamKey)> FamiliarActives = new(familiarActives);
        List<Entity> dismissedFamiliars = familiarActives.Values.Select(x => x.Familiar).ToList();
        int counter = 0;

        IEnumerable<Entity> disabledFamiliars = EntityUtilities.GetEntitiesEnumerable(familiarsQuery); // need to filter for active/dismissed familiars and not destroy them
        foreach (Entity entity in disabledFamiliars)
        {
            if (dismissedFamiliars.Contains(entity)) continue;
            else
            {
                if (entity.GetTeamEntity().Has<UserTeam>() && entity.ReadBuffer<DropTableBuffer>()[0].DropTrigger.Equals(DropTriggerType.OnSalvageDestroy))
                {
                    DestroyUtility.Destroy(EntityManager, entity);
                    counter++;
                }
            }
        }

        familiarsQuery.Dispose();
        LocalizationService.HandleReply(ctx, $"Cleared <color=white>{counter}</color> disabled familiars");
    }
}
