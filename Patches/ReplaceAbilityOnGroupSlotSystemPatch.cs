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
                    if (Plugin.UnarmedSlots.Value && entity.Read<PrefabGUID>().LookupName().ToLower().Contains("unarmed") && Core.DataStructures.PlayerSanguimancySpells.TryGetValue(steamId, out var unarmedSpells) && !unarmedSpells.Item1.Equals(0))
                    {
                        HandleUnarmed(entity, unarmedSpells);
                    }
                    else if (Plugin.WeaponShiftSlot.Value && entity.Read<PrefabGUID>().LookupName().ToLower().Contains("weapon") && Core.DataStructures.PlayerSanguimancySpells.TryGetValue(steamId, out var weaponSpell) && !weaponSpell.Item2.Equals(0))
                    {
                        HandleWeapon(entity, weaponSpell.Item2);
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

    static void HandleUnarmed(Entity entity, (int, int) spellTuple)
    {
        var buffer = entity.ReadBuffer<ReplaceAbilityOnSlotBuff>();
        if (!spellTuple.Item1.Equals(0))
        {
            ReplaceAbilityOnSlotBuff buff = new()
            {
                Slot = 1,
                NewGroupId = new(spellTuple.Item1),
                CopyCooldown = true,
                Priority = 0,
            };
            buffer.Add(buff);
        }
        if (!spellTuple.Item2.Equals(0))
        {
            ReplaceAbilityOnSlotBuff buff = new()
            {
                Slot = 4,
                NewGroupId = new(spellTuple.Item2),
                CopyCooldown = true,
                Priority = 0,
            };
            buffer.Add(buff);
        }
        if (Plugin.UnarmedShiftDash.Value)
        {
            
            ReplaceAbilityOnSlotBuff buff = new()
            {
                Slot = 3,
                NewGroupId = new(-433204738),
                CopyCooldown = true,
                Priority = 0,
            };
            buffer.Add(buff);
        }
    }
    static void HandleWeapon(Entity entity, int shiftSpell)
    {
        var buffer = entity.ReadBuffer<ReplaceAbilityOnSlotBuff>();
        ReplaceAbilityOnSlotBuff buff = new()
        {
            Slot = 3,
            NewGroupId = new(shiftSpell),
            CopyCooldown = true,
            Priority = 0,
        };
        buffer.Add(buff);
    }
    static void HandleSpells(Entity entity, ulong steamId)
    {
        if (!Core.DataStructures.PlayerBools.TryGetValue(steamId, out var bools) || !bools["SpellLock"]) return;
        if (Core.DataStructures.PlayerSanguimancySpells.TryGetValue(steamId, out var data))
        {
            var spellTuple = data;
            var buffer = entity.ReadBuffer<ReplaceAbilityOnSlotBuff>();
            foreach (var buff in buffer)
            {
                //Core.Log.LogInfo($"{buff.Slot} | {buff.ReplaceGroupId.LookupName()} | {buff.NewGroupId.LookupName()}");
                if (buff.Slot == 5)
                {
                    spellTuple = (buff.NewGroupId.GuidHash, spellTuple.Item2);
                }

                if (buff.Slot == 6)
                {
                    spellTuple = (spellTuple.Item1, buff.NewGroupId.GuidHash);
                }
            }
            Core.DataStructures.PlayerSanguimancySpells[steamId] = spellTuple;
            Core.DataStructures.SavePlayerSanguimancySpells();
        }
    }
}