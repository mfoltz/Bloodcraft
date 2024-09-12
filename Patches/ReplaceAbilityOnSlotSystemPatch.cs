using Bloodcraft.Services;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class ReplaceAbilityOnSlotSystemPatch
{
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    static readonly PrefabGUID shadowGreatSword = new(1322254792);
    static readonly PrefabGUID highlordSwordPrimary = new(-328302080);

    static readonly bool Classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;

    [HarmonyPatch(typeof(ReplaceAbilityOnSlotSystem), nameof(ReplaceAbilityOnSlotSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ReplaceAbilityOnSlotSystem __instance)
    {
        NativeArray<Entity> entities = __instance.__query_1482480545_0.ToEntityArray(Allocator.Temp); // All Components: ProjectM.EntityOwner [ReadOnly], ProjectM.Buff [ReadOnly], ProjectM.ReplaceAbilityOnSlotData [ReadOnly], ProjectM.ReplaceAbilityOnSlotBuff [Buffer] [ReadOnly], Unity.Entities.SpawnTag [ReadOnly]
        try
        {
            foreach (Entity entity in entities)
            {
                if (!Core.hasInitialized) return;

                if (entity.GetOwner().TryGetPlayer(out Entity character))
                {
                    ulong steamId = character.GetSteamId();
                    string prefabName = entity.Read<PrefabGUID>().LookupName().ToLower();

                    bool slotSpells = prefabName.Contains("unarmed") || prefabName.Contains("fishingpole");
                    bool shiftSpell = prefabName.Contains("weapon");

                    (int FirstSlot, int SecondSlot, int ShiftSlot) spells;
                    if (ConfigService.UnarmedSlots && slotSpells && steamId.TryGetPlayerSpells(out spells))
                    {
                        HandleExtraSpells(entity, steamId, spells);
                    }
                    else if (ConfigService.ShiftSlot && shiftSpell && steamId.TryGetPlayerSpells(out spells))
                    {
                        HandleShiftSpell(entity, steamId, character, spells);
                    }
                    else if (!entity.Has<WeaponLevel>() && steamId.TryGetPlayerSpells(out spells))
                    {
                        SetSpells(entity, character, steamId, spells);
                    }
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
    static void HandleExtraSpells(Entity entity, ulong steamId, (int FirstSlot, int SecondSlot, int ShiftSlot) spells)
    {
        var buffer = entity.ReadBuffer<ReplaceAbilityOnSlotBuff>();
        if (!spells.FirstSlot.Equals(0))
        {
            ReplaceAbilityOnSlotBuff buff = new()
            {
                Slot = 1,
                NewGroupId = new(spells.FirstSlot),
                CopyCooldown = true,
                Priority = 0,
            };

            buffer.Add(buff);
        }

        if (!spells.SecondSlot.Equals(0))
        {
            ReplaceAbilityOnSlotBuff buff = new()
            {
                Slot = 4,
                NewGroupId = new(spells.SecondSlot),
                CopyCooldown = true,
                Priority = 0,
            };

            buffer.Add(buff);
        }

        if (PlayerUtilities.GetPlayerBool(steamId, "ShiftLock") && !spells.ShiftSlot.Equals(0))
        {
            ReplaceAbilityOnSlotBuff buff = new()
            {
                Slot = 3,
                NewGroupId = new(spells.ShiftSlot),
                CopyCooldown = true,
                Priority = 0,
            };

            buffer.Add(buff);
        }
    }
    static void HandleShiftSpell(Entity entity, ulong steamId, Entity character, (int FirstSlot, int SecondSlot, int ShiftSlot) spells)
    {
        var buffer = entity.ReadBuffer<ReplaceAbilityOnSlotBuff>(); // prevent people switching jewels if item with spellmod is equipped?
        if (PlayerUtilities.GetPlayerBool(steamId, "ShiftLock") && !spells.ShiftSlot.Equals(0))
        {
            ReplaceAbilityOnSlotBuff buff = new()
            {
                Slot = 3,
                NewGroupId = new(spells.ShiftSlot),
                CopyCooldown = true,
                Priority = 0,
            };

            buffer.Add(buff);
        }

        /*
        Equipment equipment = character.Read<Equipment>();
        Entity weaponEntity = equipment.WeaponSlot.SlotEntity.GetEntityOnServer();
        if (weaponEntity.Has<PrefabGUID>())
        {
            PrefabGUID weaponPrefab = weaponEntity.Read<PrefabGUID>();
            if (weaponPrefab.Equals(shadowGreatSword))
            {
                PrefabGUID abilityPrefab = new(0);

                if (counter == 0)
                {
                    abilityPrefab = draculaShockwaveSlash;
                }
                else if (counter == 1)
                {
                    abilityPrefab = highlordSwordPrimary;
                }
                else if (counter == 2)
                {
                    abilityPrefab = solarusEmpoweredMelee;
                }

                ReplaceAbilityOnSlotBuff buff = new()
                {
                    Slot = 0,
                    NewGroupId = abilityPrefab,
                    CopyCooldown = true,
                    Priority = 0,
                };

                buffer.Add(buff);

                counter++;
                if (counter == 3)
                {
                    counter = 0;
                }
            }
        }
        */
    }
    static void SetSpells(Entity entity, Entity player, ulong steamId, (int FirstSlot, int SecondSlot, int ShiftSlot) spells)
    {
        bool lockSpells = PlayerUtilities.GetPlayerBool(steamId, "SpellLock");
        var buffer = entity.ReadBuffer<ReplaceAbilityOnSlotBuff>();

        foreach (var buff in buffer)
        {
            if (buff.Slot == 5)
            {
                if (lockSpells) spells = (buff.NewGroupId.GuidHash, spells.SecondSlot, spells.ShiftSlot); // then want to check on the spell in shift and get rid of it if the same prefab, same for slot 6 below
                //HandleDuplicate(entity, buff, player, steamId, spells);
            }

            if (buff.Slot == 6)
            {
                if (lockSpells) spells = (spells.FirstSlot, buff.NewGroupId.GuidHash, spells.ShiftSlot);
                //HandleDuplicate(entity, buff, player, steamId, spells);
            }
        }

        steamId.SetPlayerSpells(spells);
    }
    static void HandleDuplicate(Entity entity, ReplaceAbilityOnSlotBuff buff, Entity player, ulong steamId, (int FirstSlot, int SecondSlot, int ShiftSlot) spells)
    {
        Entity abilityGroup = ServerGameManager.GetAbilityGroup(player, 3); // get ability currently on shift, if it exists and matches what was just equipped set shift to default extra spell instead

        if (abilityGroup.Exists())
        {
            PrefabGUID abilityPrefab = abilityGroup.Read<PrefabGUID>();

            if (buff.NewGroupId == abilityPrefab)
            {
                Core.Log.LogInfo("AbilityGroup entity found, matching prefab...");
                ServerGameManager.ModifyAbilityGroupOnSlot(entity, player, 3, new(ConfigService.DefaultClassSpell));
                spells.ShiftSlot = ConfigService.DefaultClassSpell;
                steamId.SetPlayerSpells(spells);
            }
            else
            {
                Core.Log.LogInfo("AbilityGroup entity found, no matching prefab...");
            }
        }
        else
        {
            Core.Log.LogInfo("No AbilityGroup entity found...");
        }
    }
}