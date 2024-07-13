﻿using Bloodcraft.Services;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;
using ProjectM.Network;
using ProjectM.Scripting;

namespace Bloodcraft.Commands;

internal static class MiscCommands
{
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static readonly bool Leveling = Plugin.LevelingSystem.Value;
    static readonly Dictionary<PrefabGUID, int> StarterKit = new()
    {
        { new(862477668), 500 },
        { new(-1531666018), 1000 },
        { new(-1593377811), 1000 },
        { new(1821405450), 250 }
    };

    //[Command(name: "starterkit", shortHand: "kitme", adminOnly: false, usage: ".kitme", description: "Provides starting kit.")]
    public static void KitMe(ChatCommandContext ctx)
    {
        if (Core.DataStructures.PlayerBools.TryGetValue(ctx.Event.User.PlatformId, out var bools) && !bools["Kit"])
        {
            bools["Kit"] = true;
            Entity character = ctx.Event.SenderCharacterEntity;
            Core.DataStructures.SavePlayerBools();
            foreach (var item in StarterKit)
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

        if (Core.DataStructures.PlayerBools.TryGetValue(SteamID, out var bools) && Core.DataStructures.PlayerSanguimancy.TryGetValue(SteamID, out var data))
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

        if (Core.DataStructures.PlayerBools.TryGetValue(SteamID, out var bools) && Core.DataStructures.PlayerSanguimancy.TryGetValue(SteamID, out var data))
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
}