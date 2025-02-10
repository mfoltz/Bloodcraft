using Bloodcraft.Services;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Utilities;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;
using static Bloodcraft.Utilities.Misc;
using static Bloodcraft.Utilities.Misc.PlayerBoolsManager;
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

    static readonly bool _classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;

    static readonly PrefabGUID _combatBuff = new(581443919);

    public static readonly Dictionary<PrefabGUID, int> StarterKitItemPrefabGUIDs = [];

    [Command(name: "reminders", shortHand: "remindme", adminOnly: false, usage: ".remindme", description: "Toggles general reminders for various mod features.")]
    public static void LogExperienceCommand(ChatCommandContext ctx)
    {
        ulong steamId = ctx.Event.User.PlatformId;

        TogglePlayerBool(steamId, REMINDERS_KEY);
        LocalizationService.HandleReply(ctx, $"Reminders {(GetPlayerBool(steamId, REMINDERS_KEY) ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
    }

    [Command(name: "sct", adminOnly: false, usage: ".sct [Type]", description: "Toggles various scrolling text elements.")]
    public static void ToggleScrollingText(ChatCommandContext ctx, string input = "")
    {
        ulong steamId = ctx.Event.User.PlatformId;

        if (string.IsNullOrWhiteSpace(input))
        {
            ReplySCTDetails(ctx);
            return;
        }
        else if (int.TryParse(input, out int sctEnum))
        {
            sctEnum--;

            if (!Enum.IsDefined(typeof(ScrollingTextMessage), sctEnum))
            {
                ReplySCTDetails(ctx);
                return;
            }

            ScrollingTextMessage sctType = (ScrollingTextMessage)sctEnum;

            if (!ScrollingTextBoolKeyMap.TryGetValue(sctType, out var boolKey))
            {
                LocalizationService.HandleReply(ctx, "Couldn't find bool key from scrolling text type...");
                return;
            }

            TogglePlayerBool(steamId, boolKey);
            bool currentState = GetPlayerBool(steamId, boolKey);

            LocalizationService.HandleReply(ctx, $"<color=white>{sctType}</color> scrolling text {(currentState ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }
        else
        {
            if (!ScrollingTextNameMap.TryGetValue(input, out var sctType))
            {
                ReplySCTDetails(ctx);
                return;
            }

            if (!ScrollingTextBoolKeyMap.TryGetValue(sctType, out var boolKey))
            {
                LocalizationService.HandleReply(ctx, "Couldn't find bool key from scrolling text type...");
                return;
            }

            TogglePlayerBool(steamId, boolKey);
            bool currentState = GetPlayerBool(steamId, boolKey);

            LocalizationService.HandleReply(ctx, $"<color=white>{sctType}</color> scrolling text {(currentState ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }
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

        if (!GetPlayerBool(steamId, STARTER_KIT_KEY)) // if true give kit, if not no
        {
            SetPlayerBool(steamId, STARTER_KIT_KEY, true);
            Entity character = ctx.Event.SenderCharacterEntity;

            foreach (var item in StarterKitItemPrefabGUIDs)
            {
                ServerGameManager.TryAddInventoryItem(character, item.Key, item.Value);
            }

            List<string> kitItems = StarterKitItemPrefabGUIDs.Select(x => $"<color=white>{x.Key.GetLocalizedName()}</color>").ToList();

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
            ctx.Reply("You've already used your starting kit!");
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

        TogglePlayerBool(SteamID, SPELL_LOCK_KEY);

        if (GetPlayerBool(SteamID, SPELL_LOCK_KEY))
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

        User user = ctx.Event.User;
        ulong steamId = user.PlatformId;

        Entity character = ctx.Event.SenderCharacterEntity;
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
                    Classes.UpdateShift(ctx, ctx.Event.SenderCharacterEntity, spellPrefabGUID);
                }
            }

            LocalizationService.HandleReply(ctx, "Shift spell <color=green>enabled</color>.");
        }
        else
        {
            Classes.RemoveShift(ctx.Event.SenderCharacterEntity);

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

        if (ServerGameManager.HasBuff(character, _combatBuff.ToIdentifier()))
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

            TogglePlayerBool(steamId, EXO_FORM_KEY);
            ctx.Reply($"Exo form emote action (<color=white>taunt</color>) {(GetPlayerBool(steamId, EXO_FORM_KEY) ? "<color=green>enabled</color>, the Immortal King's formidable powers are now yours..." : "<color=red>disabled</color>...")}");
        }
        else
        {
            ctx.Reply("You are not yet worthy...");
        }
    }
}