using Bloodcraft.Services;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;

namespace Bloodcraft.Commands;
internal static class MiscCommands
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    static readonly PrefabGUID combatBuff = new(581443919);

    static readonly bool Leveling = Plugin.LevelingSystem.Value;
    static readonly bool StarterKit = Plugin.StarterKit.Value;
    public static Dictionary<PrefabGUID, int> KitPrefabs = [];

    [Command(name: "starterkit", shortHand: "kitme", adminOnly: false, usage: ".kitme", description: "Provides starting kit.")]
    public static void KitMe(ChatCommandContext ctx)
    {
        if (!StarterKit)
        {
            LocalizationService.HandleReply(ctx, "Starter kit is not enabled.");
            return;
        }
        if (Core.DataStructures.PlayerBools.TryGetValue(ctx.Event.User.PlatformId, out var bools) && !bools["Kit"])
        {
            bools["Kit"] = true;
            Entity character = ctx.Event.SenderCharacterEntity;
            Core.DataStructures.SavePlayerBools();
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

    [Command(name: "lockspells", shortHand: "locksp", adminOnly: false, usage: ".locksp", description: "Locks in the next spells equipped to use in your unarmed slots.")]
    public static void LockPlayerSpells(ChatCommandContext ctx)
    {
        if (!Plugin.UnarmedSlots.Value)
        {
            LocalizationService.HandleReply(ctx, "Extra spell slots for unarmed are not enabled.");
            return;
        }

        User user = ctx.Event.User;
        ulong SteamID = user.PlatformId;

        if (Core.DataStructures.PlayerBools.TryGetValue(SteamID, out var bools))
        {
            bools["SpellLock"] = !bools["SpellLock"];
            if (bools["SpellLock"])
            {
                LocalizationService.HandleReply(ctx, "Change spells to the ones you want in your unarmed slots. When done, toggle this again.");
            }
            else
            {
                LocalizationService.HandleReply(ctx, "Spells set.");
            }
            Core.DataStructures.SavePlayerBools();
        }
    }

    [Command(name: "lockshift", shortHand: "shift", adminOnly: false, usage: ".shift", description: "Locks in second spell to shift on weapons.")]
    public static void ShiftPlayerSpells(ChatCommandContext ctx)
    {
        if (!Plugin.SoftSynergies.Value && !Plugin.HardSynergies.Value)
        {
            LocalizationService.HandleReply(ctx, "Classes are not enabled and spells can't be set to shift.");
            return;
        }
        if (!Plugin.ShiftSlot.Value)
        {
            LocalizationService.HandleReply(ctx, "Shift slots are not enabled.");
            return;
        }

        User user = ctx.Event.User;
        ulong SteamID = user.PlatformId;

        if (Core.DataStructures.PlayerBools.TryGetValue(SteamID, out var bools))
        {
            bools["ShiftLock"] = !bools["ShiftLock"];
            if (bools["ShiftLock"])
            {
                LocalizationService.HandleReply(ctx, "Shift spell <color=green>enabled</color>.");
            }
            else
            {
                LocalizationService.HandleReply(ctx, "Shift spell <color=red>disabled</color>.");
            }
            Core.DataStructures.SavePlayerBools();
        }
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

        Core.CombatMusicSystem_Server.OnUpdate();

        ctx.Reply($"Combat music cleared~");
    }
}
