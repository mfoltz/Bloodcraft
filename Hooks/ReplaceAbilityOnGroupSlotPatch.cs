using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Cobalt.Hooks
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
                    if (entity.Has<EntityOwner>() && entity.Read<EntityOwner>().Owner.Has<PlayerCharacter>())
                    {
                        Entity character = entity.Read<EntityOwner>().Owner;
                        ulong steamId = character.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
                        if (entity.Read<PrefabGUID>().LookupName().ToLower().Contains("unarmed") && Core.DataStructures.PlayerSanguimancySpells.TryGetValue(steamId, out var spellTuple) && !spellTuple.Item1.IsEmpty())
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

        private static void HandleUnarmed(Entity entity, (PrefabGUID, PrefabGUID) spellTuple)
        {
            var buffer = entity.ReadBuffer<ReplaceAbilityOnSlotBuff>();
            if (!spellTuple.Item1.IsEmpty())
            {
                ReplaceAbilityOnSlotBuff buff = new()
                {
                    Slot = 5,
                    ReplaceGroupId = spellTuple.Item1,
                    CopyCooldown = true,
                    Priority = 0,
                };
                buffer.Add(buff);
            }
            if (!spellTuple.Item2.IsEmpty())
            {
                ReplaceAbilityOnSlotBuff buff = new()
                {
                    Slot = 6,
                    ReplaceGroupId = spellTuple.Item2,
                    CopyCooldown = true,
                    Priority = 0,
                };
                buffer.Add(buff);
            }
        }

        private static void HandleSpells(Entity entity, ulong steamId)
        {
            if (!Core.DataStructures.PlayerBools.TryGetValue(steamId, out var bools) || !bools["SpellLock"]) return;
            if (Core.DataStructures.PlayerSanguimancy.TryGetValue(steamId, out var data) && data.Key >= 25)
            {
                var spellTuple = Core.DataStructures.PlayerSanguimancySpells[steamId];
                var buffer = entity.ReadBuffer<ReplaceAbilityOnSlotBuff>();
                for (int i = 0; i < buffer.Length; i++)
                {
                    var buff = buffer[i];
                    if (buff.Slot == 5)
                    {
                        spellTuple.Item1 = buff.ReplaceGroupId;
                        continue;
                    }
                    if (data.Key >= 75)
                    {
                        if (buff.Slot == 6)
                        {
                            spellTuple.Item2 = buff.ReplaceGroupId;
                        }
                    }
                }
                Core.DataStructures.SavePlayerSanguimancySpells();
            }
        }
    }
}