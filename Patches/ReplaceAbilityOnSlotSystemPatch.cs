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

    static readonly bool _classes = ConfigService.ClassSystem;
    static readonly bool _unarmedSlots = ConfigService.UnarmedSlots;
    static readonly bool _duality = ConfigService.Duality;
    static readonly bool _shiftSlot = ConfigService.ShiftSlot;
    static readonly bool _shapeshiftAbilities = ConfigService.BearFormDash;

    [HarmonyPatch(typeof(ReplaceAbilityOnSlotSystem), nameof(ReplaceAbilityOnSlotSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ReplaceAbilityOnSlotSystem __instance)
    {
        if (!Core.IsReady) return;

        NativeArray<Entity> entities = __instance.__query_1482480545_0.ToEntityArray(Allocator.Temp);

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

                    bool slotSpells = prefabName.Contains("unarmed", StringComparison.CurrentCultureIgnoreCase) || prefabName.Contains("fishingpole", StringComparison.CurrentCultureIgnoreCase);
                    bool shiftSpell = prefabName.Contains("weapon", StringComparison.CurrentCultureIgnoreCase);

                    if (_unarmedSlots && slotSpells && steamId.TryGetPlayerSpells(out (int FirstUnarmed, int SecondUnarmed, int ClassSpell) spells))
                    {
                        HandleExtraSpells(entity, steamId, spells);
                    }
                    else if (_shiftSlot && shiftSpell && steamId.TryGetPlayerSpells(out spells))
                    {
                        HandleShiftSpell(entity, spells, GetPlayerBool(steamId, SHIFT_LOCK_KEY));
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
    static void HandleExtraSpells(Entity entity, ulong steamId, (int FirstUnarmed, int SecondUnarmed, int ClassSpell) spells)
    {
        var buffer = entity.ReadBuffer<ReplaceAbilityOnSlotBuff>();

        if (!spells.FirstUnarmed.Equals(0))
        {
            ReplaceAbilityOnSlotBuff buff = new()
            {
                Slot = 1,
                NewGroupId = new(spells.FirstUnarmed),
                CopyCooldown = true,
                Priority = 0,
            };

            buffer.Add(buff);
        }

        if (_duality && !spells.SecondUnarmed.Equals(0))
        {
            ReplaceAbilityOnSlotBuff buff = new()
            {
                Slot = 4,
                NewGroupId = new(spells.SecondUnarmed),
                CopyCooldown = true,
                Priority = 0,
            };

            buffer.Add(buff);
        }

        HandleShiftSpell(entity, spells, GetPlayerBool(steamId, SHIFT_LOCK_KEY));
    }
    static void HandleShiftSpell(Entity entity, (int FirstUnarmed, int SecondUnarmed, int ClassSpell) spells, bool shiftLock)
    {
        PrefabGUID spellPrefabGUID = new(spells.ClassSpell);

        if (!shiftLock) return;
        else if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(spellPrefabGUID, out Entity ability) && ability.Has<VBloodAbilityData>()) return;
        else if (spellPrefabGUID.HasValue())
        {
            var buffer = entity.ReadBuffer<ReplaceAbilityOnSlotBuff>();
            ReplaceAbilityOnSlotBuff buff = new()
            {
                Slot = 3,
                NewGroupId = new(spells.ClassSpell),
                CopyCooldown = true,
                Priority = 0,
            };

            buffer.Add(buff);
        }
    }
    static void SetSpells(Entity entity, ulong steamId, (int FirstUnarmed, int SecondUnarmed, int ClassSpell) spells)
    {
        bool lockSpells = GetPlayerBool(steamId, SPELL_LOCK_KEY);
        var buffer = entity.ReadBuffer<ReplaceAbilityOnSlotBuff>();

        foreach (var buff in buffer)
        {
            if (buff.Slot == 5)
            {
                if (lockSpells) spells = (buff.NewGroupId.GuidHash, spells.SecondUnarmed, spells.ClassSpell);
            }

            if (buff.Slot == 6)
            {
                if (lockSpells) spells = (spells.FirstUnarmed, buff.NewGroupId.GuidHash, spells.ClassSpell);
            }
        }

        steamId.SetPlayerSpells(spells);
    }
}