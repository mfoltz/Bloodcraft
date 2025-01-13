using Bloodcraft.Services;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using static Bloodcraft.Utilities.Misc.PlayerBoolsManager;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class ReplaceAbilityOnSlotSystemPatch
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static PrefabCollectionSystem PrefabCollectionSystem => SystemService.PrefabCollectionSystem;
    static ActivateVBloodAbilitySystem ActivateVBloodAbilitySystem => SystemService.ActivateVBloodAbilitySystem;

    static readonly bool _classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;
    static readonly bool _unarmedSlots = ConfigService.UnarmedSlots;
    static readonly bool _shiftSlot = ConfigService.ShiftSlot;
    static readonly bool _shapeshiftAbilities = ConfigService.ShapeshiftAbilities;

    static readonly PrefabGUID _vBloodAbilityReplaceBuff = new(1171608023);
    static readonly PrefabGUID _bearFormBuff = new(-1569370346);
    static readonly PrefabGUID _wolfFormBuff = new(-351718282);

    static readonly PrefabGUID _bearDashAbility = new(1873182450);
    static readonly PrefabGUID _wolfBiteAbility = new(-1262842180);

    [HarmonyPatch(typeof(ReplaceAbilityOnSlotSystem), nameof(ReplaceAbilityOnSlotSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ReplaceAbilityOnSlotSystem __instance)
    {
        if (!Core._initialized) return;

        NativeArray<Entity> entities = __instance.__query_1482480545_0.ToEntityArray(Allocator.Temp); // All Components: ProjectM.EntityOwner [ReadOnly], ProjectM.Buff [ReadOnly], ProjectM.ReplaceAbilityOnSlotData [ReadOnly], ProjectM.ReplaceAbilityOnSlotBuff [Buffer] [ReadOnly], Unity.Entities.SpawnTag [ReadOnly]
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out EntityOwner entityOwner) || !entityOwner.Owner.Exists()) continue;
                else if (entityOwner.Owner.TryGetPlayer(out Entity character))
                {
                    ulong steamId = character.GetSteamId();

                    PrefabGUID prefabGuid = entity.GetPrefabGuid();
                    string prefabName = prefabGuid.GetPrefabName();

                    bool slotSpells = prefabName.Contains("unarmed", StringComparison.OrdinalIgnoreCase) || prefabName.Contains("fishingpole", StringComparison.OrdinalIgnoreCase);
                    bool shiftSpell = prefabName.Contains("weapon", StringComparison.OrdinalIgnoreCase);

                    (int FirstSlot, int SecondSlot, int ShiftSlot) spells;
                    if (_unarmedSlots && slotSpells && steamId.TryGetPlayerSpells(out spells))
                    {
                        HandleExtraSpells(entity, steamId, spells);
                    }
                    else if (_shiftSlot && shiftSpell && steamId.TryGetPlayerSpells(out spells))
                    {
                        HandleShiftSpell(entity, spells, GetPlayerBool(steamId, "ShiftLock"));
                    }
                    else if (_shapeshiftAbilities)
                    {
                        if (prefabGuid.Equals(_bearFormBuff)) HandleBearDash(entity);
                        else if (prefabGuid.Equals(_wolfFormBuff)) HandleWolfBite(entity);
                    }
                    else if (!entity.Has<WeaponLevel>() && steamId.TryGetPlayerSpells(out spells))
                    {
                        SetSpells(entity, steamId, spells);
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

        HandleShiftSpell(entity, spells, GetPlayerBool(steamId, "ShiftLock"));
    }
    static void HandleBearDash(Entity entity)
    {
        if (entity.TryGetBuffer<ReplaceAbilityOnSlotBuff>(out var buffer))
        {
            ReplaceAbilityOnSlotBuff buff = new()
            {
                Slot = 3,
                NewGroupId = _bearDashAbility,
                CopyCooldown = true,
                Priority = 99,
                CastBlockType = GroupSlotModificationCastBlockType.WholeCast
            };

            buffer.Add(buff);
        }
    }
    static void HandleWolfBite(Entity entity)
    {
        if (entity.TryGetBuffer<ReplaceAbilityOnSlotBuff>(out var buffer))
        {
            ReplaceAbilityOnSlotBuff buff = new()
            {
                Slot = 0,
                NewGroupId = _wolfBiteAbility,
                CopyCooldown = true,
                Priority = 99,
                CastBlockType = GroupSlotModificationCastBlockType.WholeCast
            };

            // 4294967221
            // AbilityTypeFlag
            // AbilityKit, Interact_BreakMount, AbilityKit_IgnoreInCombat, IgnoreSpellBlock
            // bear and wolf both have Demount

            buffer.Add(buff);
        }
    }
    static void HandleShiftSpell(Entity entity, (int FirstSlot, int SecondSlot, int ShiftSlot) spells, bool shiftLock)
    {
        PrefabGUID spellPrefabGUID = new(spells.ShiftSlot);

        if (!shiftLock) return;
        else if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(spellPrefabGUID, out Entity ability) && ability.Has<VBloodAbilityData>()) return;
        else if (spellPrefabGUID.HasValue())
        {
            var buffer = entity.ReadBuffer<ReplaceAbilityOnSlotBuff>();
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
    static void SetSpells(Entity entity, ulong steamId, (int FirstSlot, int SecondSlot, int ShiftSlot) spells)
    {
        bool lockSpells = GetPlayerBool(steamId, "SpellLock");
        var buffer = entity.ReadBuffer<ReplaceAbilityOnSlotBuff>();

        foreach (var buff in buffer)
        {
            if (buff.Slot == 5)
            {
                if (lockSpells) spells = (buff.NewGroupId.GuidHash, spells.SecondSlot, spells.ShiftSlot); // then want to check on the spell in shift and get rid of it if the same prefab, same for slot 6 below
            }

            if (buff.Slot == 6)
            {
                if (lockSpells) spells = (spells.FirstSlot, buff.NewGroupId.GuidHash, spells.ShiftSlot);
            }
        }

        steamId.SetPlayerSpells(spells);
    }
}