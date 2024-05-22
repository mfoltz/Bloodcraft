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
                if (Plugin.ExpertiseSystem.Value && Plugin.Sanguimancy.Value && entity.Has<EntityOwner>() && entity.Read<EntityOwner>().Owner.Has<PlayerCharacter>())
                {
                    Entity character = entity.Read<EntityOwner>().Owner;
                    ulong steamId = character.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
                    if (entity.Read<PrefabGUID>().LookupName().ToLower().Contains("unarmed") && Core.DataStructures.PlayerSanguimancySpells.TryGetValue(steamId, out var spellTuple) && !spellTuple.Item1.Equals(0))
                    {
                        HandleUnarmed(entity, spellTuple);
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
    }
    static void HandleSpells(Entity entity, ulong steamId)
    {
        if (!Core.DataStructures.PlayerBools.TryGetValue(steamId, out var bools) || !bools["SpellLock"]) return;
        if (Core.DataStructures.PlayerSanguimancy.TryGetValue(steamId, out var data) && data.Key >= Plugin.FirstSlot.Value)
        {
            var spellTuple = Core.DataStructures.PlayerSanguimancySpells[steamId];
            var buffer = entity.ReadBuffer<ReplaceAbilityOnSlotBuff>();
            foreach (var buff in buffer)
            {
                //Core.Log.LogInfo($"{buff.Slot} | {buff.ReplaceGroupId.LookupName()} | {buff.NewGroupId.LookupName()}");
                if (buff.Slot == 5)
                {
                    spellTuple = (buff.NewGroupId.GuidHash, spellTuple.Item2);
                }

                if (data.Key >= Plugin.SecondSlot.Value && buff.Slot == 6)
                {
                    spellTuple = (spellTuple.Item1, buff.NewGroupId.GuidHash);
                }
            }
            Core.DataStructures.PlayerSanguimancySpells[steamId] = spellTuple;
            Core.DataStructures.SavePlayerSanguimancySpells();
        }
    }
}