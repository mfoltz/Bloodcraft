using Bloodstone.API;
using ProjectM;
using ProjectM.Network;
using System.Runtime.CompilerServices;
using Unity.Entities;
using VampireCommandFramework;
using VCreate.Core.Converters;
using VCreate.Core.Toolbox;
using static VCreate.Core.Services.PlayerService;
using VCreate.Data;
using UnityEngine;
using VCreate.Systems;
using ProjectM.Scripting;
using static Il2CppSystem.Data.Common.ObjectStorage;
using UnityEngine.Experimental.GlobalIllumination;
using Unity.Collections;
using Unity.Properties;
using ProjectM.Presentation;
using ProjectM.Gameplay.Systems;

namespace VCreate.Core.Commands
{
    internal class GiveItemCommands
    {
        [Command(name: "give", shortHand: "gv", adminOnly: true, usage: ".gv [ItemName] [Quantity]", description: "Gives the specified item w/quantity.")]
        public static void GiveItem(ChatCommandContext ctx, GivenItem item, int quantity = 1)
        {
            if (Helper.AddItemToInventory(ctx.Event.SenderCharacterEntity, item.Value, quantity, out var _))
            {
                var prefabSys = VWorld.Server.GetExistingSystem<PrefabCollectionSystem>();
                prefabSys.PrefabGuidToNameDictionary.TryGetValue(item.Value, out var name);
                ctx.Reply($"Gave {quantity} {name}");
            }
        }

        public record struct GivenItem(PrefabGUID Value);

        internal class GiveItemConverter : CommandArgumentConverter<GivenItem>
        {
            public override GivenItem Parse(ICommandContext ctx, string input)
            {
                PrefabGUID prefabGUID;
                if (Helper.TryGetItemPrefabGUIDFromString(input, out prefabGUID))
                    return new GivenItem(prefabGUID);
                throw ctx.Error("Could not find item: " + input);
            }
        }
    }

    internal class ReviveCommands
    {
        [Command(name: "revive", shortHand: "rev", adminOnly: true, usage: ".rev [PlayerName]", description: "Revives self or player.")]
        public static void ReviveCommand(ChatCommandContext ctx, FoundPlayer player = null)
        {
            var Character = player?.Value.Character ?? ctx.Event.SenderCharacterEntity;
            var User = player?.Value.User ?? ctx.Event.SenderUserEntity;

            Helper.ReviveCharacter(Character, User);

            ctx.Reply("Revived");
        }
    }

    internal class DebugCommands
    {
        [Command(name: "listResistCategories", shortHand: "resists", adminOnly: true, usage: ".resists", description: "Lists current resist damage category stats.")]
        public static void ReviveCommand(ChatCommandContext ctx)
        {
            var Character = ctx.Event.SenderCharacterEntity;
            ResistCategoryStats resistCategoryStats = Character.Read<ResistCategoryStats>();
            ctx.Reply($"Resist Versus Player Vampires: {resistCategoryStats.ResistVsPlayerVampires._Value}");
        }
    }

    internal class MiscCommands
    {
        [Command(name: "demigod", shortHand: "deus", adminOnly: true, usage: ".deus", description: "Activates demigod mode. Use again to clear.")]
        public static void DemigodCommand(ChatCommandContext ctx)
        {
            PrefabGUID ignored = new(-1430861195);
            PrefabGUID playerfaction = new(1106458752);

            BuffUtility.BuffSpawner buffSpawner = BuffUtility.BuffSpawner.Create(VWorld.Server.GetExistingSystem<ServerScriptMapper>()._ServerGameManager);
            EntityCommandBufferSystem entityCommandBufferSystem = VWorld.Server.GetExistingSystem<EntityCommandBufferSystem>();
            EntityCommandBuffer entityCommandBuffer = entityCommandBufferSystem.CreateCommandBuffer();
            Entity character = ctx.Event.SenderCharacterEntity;
            ulong SteamID = ctx.Event.User.PlatformId;
            BufferFromEntity<BuffBuffer> buffBuffer = VWorld.Server.EntityManager.GetBufferFromEntity<BuffBuffer>(false);
            bool check = BuffUtility.TryGetBuff(character, VCreate.Data.Buffs.Admin_Invulnerable_Buff, buffBuffer, out var buff);
            if (check)
            {
                BuffUtility.TryRemoveBuff(ref buffSpawner, entityCommandBuffer, VCreate.Data.Buffs.Admin_Invulnerable_Buff, character);
                // also set faction back to players
                FactionReference factionReference = character.Read<FactionReference>();
                factionReference.FactionGuid._Value = playerfaction;
                character.Write(factionReference);
                ctx.Reply("Demigod mode disabled.");
            }
            else
            {
                OnHover.BuffNonPlayer(ctx.Event.SenderCharacterEntity, VCreate.Data.Buffs.Admin_Invulnerable_Buff);
                // also set faction to ignored
                FactionReference factionReference = character.Read<FactionReference>();
                factionReference.FactionGuid._Value = ignored;
                character.Write(factionReference);
                ctx.Reply("Demigod mode enabled. (can't be harmed, enemies ignore you)");
            }
        }

        [Command(name: "unlock", shortHand: "ul", adminOnly: true, usage: ".ul [PlayerName]", description: "Unlocks vBloods, research and achievements.")]
        public static void UnlockCommand(ChatCommandContext ctx, string playerName)
        {
            TryGetCharacterFromName(playerName, out Entity character);
            TryGetUserFromName(playerName, out Entity user);
            try
            {
                VWorld.Server.GetExistingSystem<DebugEventsSystem>();
                FromCharacter fromCharacter = new FromCharacter()
                {
                    User = user,
                    Character = character
                };

                Helper.UnlockVBloods(fromCharacter);

                Helper.UnlockResearch(fromCharacter);

                Helper.UnlockAchievements(fromCharacter);
            }
            catch (Exception ex)
            {
                throw ctx.Error(ex.ToString());
            }
        }

        [Command(name: "bloodMerlot", shortHand: "bm", adminOnly: true, usage: ".bm [Type] [Quantity] [Quality]", description: "Provides a blood merlot as ordered.")]
        public static void GiveBloodPotionCommand(ChatCommandContext ctx, VCreate.Data.Prefabs.BloodType type = VCreate.Data.Prefabs.BloodType.frailed, int quantity = 1, float quality = 100f)
        {
            quality = Mathf.Clamp(quality, 0, 100);
            int i;
            for (i = 0; i < quantity; i++)
            {
                if (Helper.AddItemToInventory(ctx.Event.SenderCharacterEntity, Prefabs.Item_Consumable_PrisonPotion_Bloodwine, 1, out var bloodPotionEntity))
                {
                    var blood = new StoredBlood()
                    {
                        BloodQuality = quality,
                        BloodType = new PrefabGUID((int)type)
                    };

                    bloodPotionEntity.Write(blood);
                }
                else
                {
                    break;
                }
            }

            ctx.Reply($"Got {i} Blood Potion(s) Type <color=#ff0>{type}</color> with <color=#ff0>{quality}</color>% quality");
        }

        [Command(name: "ping", shortHand: "!", adminOnly: false, usage: ".!", description: "Displays user ping.")]
        public static void PingCommand(ChatCommandContext ctx)
        {
            var ping = (int)(ctx.Event.SenderCharacterEntity.Read<Latency>().Value * 1000);
            ctx.Reply($"Your latency is <color=#ffff00>{ping}</color>ms");
        }
    }
}