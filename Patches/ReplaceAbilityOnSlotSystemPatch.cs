using Bloodcraft.Services;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class ReplaceAbilityOnSlotSystemPatch
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static PrefabCollectionSystem PrefabCollectionSystem => SystemService.PrefabCollectionSystem;
    static ActivateVBloodAbilitySystem ActivateVBloodAbilitySystem => SystemService.ActivateVBloodAbilitySystem;

    static readonly bool Classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;

    static readonly PrefabGUID VBloodAbilityBuff = new(1171608023);

    [HarmonyPatch(typeof(ReplaceAbilityOnSlotSystem), nameof(ReplaceAbilityOnSlotSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ReplaceAbilityOnSlotSystem __instance)
    {
        if (!Core.hasInitialized) return;

        NativeArray<Entity> entities = __instance.__query_1482480545_0.ToEntityArray(Allocator.Temp); // All Components: ProjectM.EntityOwner [ReadOnly], ProjectM.Buff [ReadOnly], ProjectM.ReplaceAbilityOnSlotData [ReadOnly], ProjectM.ReplaceAbilityOnSlotBuff [Buffer] [ReadOnly], Unity.Entities.SpawnTag [ReadOnly]
        try
        {
            foreach (Entity entity in entities)
            {
                if (entity.GetOwner().TryGetPlayer(out Entity character))
                {
                    ulong steamId = character.GetSteamId();
                    string prefabName = entity.Read<PrefabGUID>().LookupName().ToLower();

                    bool slotSpells = prefabName.Contains("unarmed") || prefabName.Contains("fishingpole");
                    bool shiftSpell = prefabName.Contains("weapon");

                    (int FirstSlot, int SecondSlot, int ShiftSlot) spells;
                    if (ConfigService.UnarmedSlots && slotSpells && steamId.TryGetPlayerSpells(out spells))
                    {
                        HandleExtraSpells(entity, character, steamId, spells);
                    }
                    else if (ConfigService.ShiftSlot && shiftSpell && steamId.TryGetPlayerSpells(out spells))
                    {
                        HandleShiftSpell(entity, character, spells, PlayerUtilities.GetPlayerBool(steamId, "ShiftLock"));
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
    static void HandleExtraSpells(Entity entity, Entity character, ulong steamId, (int FirstSlot, int SecondSlot, int ShiftSlot) spells)
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

        HandleShiftSpell(entity, character, spells, PlayerUtilities.GetPlayerBool(steamId, "ShiftLock"));    
    }
    static void HandleShiftSpell(Entity entity, Entity character, (int FirstSlot, int SecondSlot, int ShiftSlot) spells, bool shiftLock)
    {
        PrefabGUID spellPrefabGUID = new(spells.ShiftSlot);

        if (!shiftLock) return;
        else if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(spellPrefabGUID, out Entity ability) && ability.TryGetComponent(out VBloodAbilityData vBloodAbilityData))
        {
            Entity abilityGroup = ServerGameManager.GetAbilityGroup(character, 3);
            if (abilityGroup.Exists() && ServerGameManager.TryGetBuffer<VBloodAbilityBuffEntry>(character, out var firstBuffer))
            {
                Entity buffEntity = Entity.Null;
                PrefabGUID oldAbility = abilityGroup.Read<PrefabGUID>();
                int index = -1;

                for (int i = 0; i < firstBuffer.Length; i++)
                {
                    VBloodAbilityBuffEntry vBloodAbilityBuffEntry = firstBuffer[i];
                    if (vBloodAbilityBuffEntry.ActiveAbility.Equals(oldAbility) && vBloodAbilityBuffEntry.SlotId == 3)
                    {
                        buffEntity = vBloodAbilityBuffEntry.ActiveBuff;
                        index = i;
                        //Core.Log.LogInfo("Found old shift ability...");
                        break;
                    }
                }

                if (firstBuffer.IsIndexWithinRange(index) && buffEntity.Exists())
                {
                    firstBuffer.RemoveAt(index);
                    DestroyUtility.Destroy(EntityManager, buffEntity, DestroyDebugReason.TryRemoveBuff);
                }
            }
            
            VBloodAbilityUtilities.InstantiateBuff(EntityManager, ActivateVBloodAbilitySystem._BuffSpawnerSystemData, character, PrefabCollectionSystem._PrefabGuidToEntityMap[VBloodAbilityBuff], spellPrefabGUID, 3);
            if (ServerGameManager.TryGetBuffer<VBloodAbilityBuffEntry>(character, out var secondBuffer))
            {
                foreach(VBloodAbilityBuffEntry abilityEntry in secondBuffer)
                {
                    if (abilityEntry.ActiveAbility.Equals(spellPrefabGUID))
                    {
                        //Core.Log.LogInfo("Set correct ability type for shift...");
                        abilityEntry.ActiveBuff.With((ref VBloodAbilityReplaceBuff vBloodAbilityReplaceBuff) =>
                        {
                            vBloodAbilityReplaceBuff.AbilityType = vBloodAbilityData.AbilityType;
                        });
                        break;
                    }
                }
            }       
        }
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
}