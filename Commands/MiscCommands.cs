using Bloodcraft.Services;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;
using static Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarUnlocksManager;
using static Bloodcraft.Utilities.Misc;
using static Bloodcraft.Utilities.Misc.PlayerBoolsManager;
using static VCF.Core.Basics.RoleCommands;

namespace Bloodcraft.Commands;

[CommandGroup(name: "miscellaneous", "misc")]
internal static class MiscCommands
{
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static CombatMusicSystem_Server CombatMusicSystemServer => SystemService.CombatMusicSystem_Server;
    static ClaimAchievementSystem ClaimAchievementSystem => SystemService.ClaimAchievementSystem;
    static EntityCommandBufferSystem EntityCommandBufferSystem => SystemService.EntityCommandBufferSystem;

    static readonly PrefabGUID _combatBuff = new(581443919);

    public static readonly Dictionary<PrefabGUID, int> StarterKitItemPrefabGUIDs = [];

    [Command(name: "reminders", shortHand: "remindme", adminOnly: false, usage: ".misc remindme", description: "Toggles general reminders for various mod features.")]
    public static void LogExperienceCommand(ChatCommandContext ctx)
    {
        ulong steamId = ctx.Event.User.PlatformId;

        TogglePlayerBool(steamId, REMINDERS_KEY);
        LocalizationService.HandleReply(ctx, $"Reminders {(GetPlayerBool(steamId, REMINDERS_KEY) ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
    }

    [Command(name: "sct", adminOnly: false, usage: ".misc sct [Type]", description: "Toggles various scrolling text elements.")]
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

    [Command(name: "starterkit", shortHand: "kitme", adminOnly: false, usage: ".misc kitme", description: "Provides starting kit.")]
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

            string kitFamiliarName = string.Empty;
            PrefabGUID familiarPrefabGuid = new(ConfigService.KitFamiliar);

            if (familiarPrefabGuid.HasValue()
                && familiarPrefabGuid.IsCharacter())
            {
                FamiliarUnlocksData unlocksData = LoadFamiliarUnlocksData(steamId);
                string boxName = steamId.TryGetFamiliarBox(out var currentBox) ? currentBox : string.Empty;

                if (string.IsNullOrEmpty(boxName) || !unlocksData.FamiliarUnlocks.ContainsKey(boxName))
                {
                    boxName = unlocksData.FamiliarUnlocks.Count == 0 ? "box1" : unlocksData.FamiliarUnlocks.Keys.First();
                    if (!unlocksData.FamiliarUnlocks.ContainsKey(boxName))
                    {
                        unlocksData.FamiliarUnlocks[boxName] = [];
                    }

                    steamId.SetFamiliarBox(boxName);
                }

                if (!unlocksData.FamiliarUnlocks.TryGetValue(boxName, out var familiars))
                {
                    familiars = [];
                    unlocksData.FamiliarUnlocks[boxName] = familiars;
                }

                int familiarGuid = familiarPrefabGuid.GuidHash;

                if (!familiars.Contains(familiarGuid))
                {
                    familiars.Add(familiarGuid);
                    SaveFamiliarUnlocksData(steamId, unlocksData);
                    kitFamiliarName = new PrefabGUID(familiarGuid).GetLocalizedName();
                }
            }

            List<string> kitItems = [..StarterKitItemPrefabGUIDs.Select(x => $"<color=#ffd9eb>{x.Key.GetLocalizedName()}</color>x<color=white>{x.Value}</color>")];

            LocalizationService.HandleReply(ctx, "You've received a <color=yellow>starter kit</color>:");

            const int maxPerMessage = 6;
            for (int i = 0; i < kitItems.Count; i += maxPerMessage)
            {
                var batch = kitItems.Skip(i).Take(maxPerMessage);
                string items = string.Join(", ", batch);

                LocalizationService.HandleReply(ctx, $"{items}");
            }

            if (!string.IsNullOrEmpty(kitFamiliarName))
            {
                LocalizationService.HandleReply(ctx, $"<color=green>{kitFamiliarName}</color>");
            }
        }
        else
        {
            ctx.Reply("You've already used the <color=white>starter kit</color>!");
        }
    }

    [Command(name: "prepareforthehunt", shortHand: "prepare", adminOnly: false, usage: ".misc prepare", description: "Completes GettingReadyForTheHunt if not already completed.")]
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

    [Command(name: "userstats", adminOnly: false, usage: ".misc userstats", description: "Shows neat information about the player.")]
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

    [Command(name: "silence", adminOnly: false, usage: ".misc silence", description: "Resets stuck combat music if needed.")]
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
        ctx.Reply($"Combat music cleared!");
    }
}