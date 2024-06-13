using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class ReplaceAbilityOnGroupSlotSystemPatch
{
    public static Dictionary<PrefabGUID, int> ClassSpells = [];

    [HarmonyPatch(typeof(ReplaceAbilityOnSlotSystem), nameof(ReplaceAbilityOnSlotSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ReplaceAbilityOnSlotSystem __instance)
    {
        NativeArray<Entity> entities = __instance.__query_1482480545_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (entity.Has<EntityOwner>() && entity.Read<EntityOwner>().Owner.Has<PlayerCharacter>())
                {
                    Entity character = entity.Read<EntityOwner>().Owner;
                    ulong steamId = character.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
                    if (Plugin.UnarmedSlots.Value && entity.Read<PrefabGUID>().GetPrefabName().ToLower().Contains("unarmed") && Core.DataStructures.PlayerSpells.TryGetValue(steamId, out var unarmedSpells))
                    {
                        HandleUnarmed(entity, character, unarmedSpells, steamId);
                    }
                    else if (Plugin.PrestigeSystem.Value && entity.Read<PrefabGUID>().GetPrefabName().ToLower().Contains("weapon") && Core.DataStructures.PlayerSpells.TryGetValue(steamId, out var playerSpells) && !playerSpells.ClassSpell.Equals(0))
                    {
                        HandleWeapon(entity, character, steamId, playerSpells);
                    }
                    else if (!entity.Has<WeaponLevel>())
                    {
                        HandleSpells(entity, steamId);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Core.Log.LogError($"Error in ReplaceAbilityOnGroupSlotPatch: {e}");
        }
        finally
        {
            entities.Dispose();
        }
    }

    static void HandleUnarmed(Entity entity, Entity player, (int, int, int) playerSpells, ulong steamId)
    {
        var buffer = entity.ReadBuffer<ReplaceAbilityOnSlotBuff>();
        if (!playerSpells.Item1.Equals(0))
        {
            ReplaceAbilityOnSlotBuff buff = new()
            {
                Slot = 1,
                NewGroupId = new(playerSpells.Item1),
                CopyCooldown = true,
                Priority = 0,
            };
            buffer.Add(buff);
        }
        if (!playerSpells.Item2.Equals(0))
        {
            ReplaceAbilityOnSlotBuff buff = new()
            {
                Slot = 4,
                NewGroupId = new(playerSpells.Item2),
                CopyCooldown = true,
                Priority = 0,
            };
            buffer.Add(buff);
        }
        if (!Core.DataStructures.PlayerBools.TryGetValue(steamId, out var bools) || !bools["ShiftLock"]) return;

        if (!playerSpells.Item3.Equals(0))
        {
            PrefabGUID PrefabGUID = new(playerSpells.Item3);
            ReplaceAbilityOnSlotBuff buff = new()
            {
                Slot = 3,
                NewGroupId = PrefabGUID,
                CopyCooldown = true,
                Priority = 0,
            };
            buffer.Add(buff);
        }
    }
    static void HandleWeapon(Entity entity, Entity player, ulong steamId, (int, int, int) playerSpells)
    {
        if (!Core.DataStructures.PlayerBools.TryGetValue(steamId, out var bools) || !bools["ShiftLock"]) return;
        var buffer = entity.ReadBuffer<ReplaceAbilityOnSlotBuff>(); // prevent people switching jewels if item with spellmod is equipped?

        if (!playerSpells.Item3.Equals(0))
        {
            PrefabGUID PrefabGUID = new(playerSpells.Item3);
            ReplaceAbilityOnSlotBuff buff = new()
            {
                Slot = 3,
                NewGroupId = PrefabGUID,
                CopyCooldown = true,
                Priority = 0,
            };
            buffer.Add(buff);
        }
    }
    static void HandleSpells(Entity entity, ulong steamId)
    {
        if (!Core.DataStructures.PlayerBools.TryGetValue(steamId, out var bools) || !bools["SpellLock"]) return;
        if (Core.DataStructures.PlayerSpells.TryGetValue(steamId, out var data))
        {
            var spellTuple = data;
            var buffer = entity.ReadBuffer<ReplaceAbilityOnSlotBuff>();
            foreach (var buff in buffer)
            {
                //Core.Log.LogInfo($"{buff.Slot} | {buff.ReplaceGroupId.GetPrefabName()} | {buff.NewGroupId.GetPrefabName()}");
                if (buff.Slot == 5)
                {
                    spellTuple = (buff.NewGroupId.GuidHash, spellTuple.SecondUnarmed, spellTuple.ClassSpell);
                }

                if (buff.Slot == 6)
                {
                    spellTuple = (spellTuple.FirstUnarmed, buff.NewGroupId.GuidHash, spellTuple.ClassSpell);
                }
            }
            Core.DataStructures.PlayerSpells[steamId] = spellTuple;
            Core.DataStructures.SavePlayerSpells();
        }
    }

    [HarmonyPatch(typeof(AbilityRunScriptsSystem), nameof(AbilityRunScriptsSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(AbilityRunScriptsSystem __instance)
    {
        NativeArray<Entity> entities = __instance._OnPostCastFinishedQuery.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {   
                AbilityPostCastFinishedEvent postCast = entity.Read<AbilityPostCastFinishedEvent>();
                PrefabGUID abilityGroupPrefab = postCast.AbilityGroup.Read<PrefabGUID>();
                if (postCast.Character.Has<PlayerCharacter>() && ClassSpells.ContainsKey(abilityGroupPrefab))
                {
                    float cooldown = ClassSpells[abilityGroupPrefab].Equals(0) ? 8f : ClassSpells[abilityGroupPrefab] * 15f;
                    Core.ServerGameManager.SetAbilityGroupCooldown(postCast.Character, abilityGroupPrefab, cooldown);
                }
            }
        }
        catch (System.Exception e)
        {
            Core.Log.LogError($"Error in AbilityRunScriptsSystem: {e}");
        }
        finally
        {
            entities.Dispose();
        }
    }
}