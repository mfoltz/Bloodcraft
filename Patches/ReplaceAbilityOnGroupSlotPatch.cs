using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches
{
    [HarmonyPatch(typeof(ReplaceAbilityOnSlotSystem), nameof(ReplaceAbilityOnSlotSystem.OnUpdate))]
    public static class ReplaceAbilityOnGroupSlotPatch
    {
        [HarmonyPrefix]
        public static void OnUpdatePrefix(ReplaceAbilityOnSlotSystem __instance)
        {
            //Core.Log.LogInfo("ReplaceAbilityOnGroupSlotPatch");
            NativeArray<Entity> entities = __instance.__query_1482480545_0.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (Entity entity in entities)
                {
                    if (Plugin.Sanguimancy.Value && entity.Has<EntityOwner>() && entity.Read<EntityOwner>().Owner.Has<PlayerCharacter>())
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

        private static void HandleUnarmed(Entity entity, (int, int) spellTuple)
        {
            var buffer = entity.ReadBuffer<ReplaceAbilityOnSlotBuff>();
            if (!spellTuple.Item1.Equals(0))
            {
                ReplaceAbilityOnSlotBuff buff = new()
                {
                    Slot = 5,
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
                    Slot = 6,
                    NewGroupId = new(spellTuple.Item2),
                    CopyCooldown = true,
                    Priority = 0,
                };
                buffer.Add(buff);
            }
        }

        private static void HandleSpells(Entity entity, ulong steamId)
        {
            Core.Log.LogInfo("HandleSpells...");
            if (!Core.DataStructures.PlayerBools.TryGetValue(steamId, out var bools) || !bools["SpellLock"]) return;
            Core.Log.LogInfo("Passed lock check...");
            if (Core.DataStructures.PlayerSanguimancy.TryGetValue(steamId, out var data) && data.Key >= 25)
            {
                Core.Log.LogInfo("Passed sanguimancy check...");
                var spellTuple = Core.DataStructures.PlayerSanguimancySpells[steamId];
                var buffer = entity.ReadBuffer<ReplaceAbilityOnSlotBuff>();
                for (int i = 0; i < buffer.Length; i++)
                {
                    //Core.Log.LogInfo($"{buffer[i].Slot} | {buffer[i].ReplaceGroupId.LookupName()} | {buffer[i].NewGroupId.LookupName()}");
                    var buff = buffer[i];
                    if (!buff.Slot.Equals(5) && !buff.Slot.Equals(6)) continue;
                    if (buff.Slot == 5)
                    {
                        spellTuple.Item1 = buff.ReplaceGroupId.GuidHash;
                        continue;
                    }
                    if (data.Key >= 75)
                    {
                        if (buff.Slot == 6)
                        {
                            spellTuple.Item2 = buff.ReplaceGroupId.GuidHash;
                        }
                    }
                }
                Core.DataStructures.SavePlayerSanguimancySpells();
            }
        }
    }
}