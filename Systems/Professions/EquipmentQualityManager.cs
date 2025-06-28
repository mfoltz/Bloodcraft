using Bloodcraft.Interfaces;
using ProjectM;
using ProjectM.Scripting;
using ProjectM.Shared;
using Unity.Entities;

namespace Bloodcraft.Systems.Professions;
internal static class EquipmentQualityManager
{
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    const float MAX_DURABILITY_BONUS = 1f;
    const float MAX_WEAPON_BONUS = 0.1f;
    const float MAX_ARMOR_BONUS = 0.1f;
    const float MAX_MAGIC_BONUS = 0.1f;
    const int MAX_PROFESSION_LEVEL = 100;
    public static void ApplyPlayerEquipmentStats(ulong steamId, Entity equipmentEntity)
    {
        if (!equipmentEntity.Exists()) return;
        IProfession handler = ProfessionFactory.GetProfession(equipmentEntity.GetPrefabGuid());

        int professionLevel = handler.GetProfessionData(steamId).Key;
        float scaledBonus = 0f;

        if (equipmentEntity.Has<Durability>())
        {
            scaledBonus = CalculateDurabilityBonus(professionLevel);

            equipmentEntity.With((ref Durability durability) =>
            {
                durability.MaxDurability *= scaledBonus;
                durability.Value = durability.MaxDurability;
            });
        }

        if (!ServerGameManager.TryGetBuffer<ModifyUnitStatBuff_DOTS>(equipmentEntity, out var buffer)) return;

        scaledBonus = CalculateStatBonus(handler, professionLevel);

        for (int i = 0; i < buffer.Length; i++)
        {
            ModifyUnitStatBuff_DOTS statBuff = buffer[i];
            if (statBuff.StatType.Equals(UnitStatType.InventorySlots)) continue;

            statBuff.Value *= scaledBonus;
            buffer[i] = statBuff;
        }
    }
    public static void ApplyFamiliarEquipmentStats(int professionLevel, int currentDurability, Entity equipmentEntity)
    {
        IProfession handler = ProfessionFactory.GetProfession(equipmentEntity.GetPrefabGuid());
        float scaledBonus = 0f;

        if (equipmentEntity.Has<Durability>())
        {
            scaledBonus = CalculateDurabilityBonus(professionLevel);

            equipmentEntity.With((ref Durability durability) =>
            {
                durability.MaxDurability *= scaledBonus;
                durability.Value = (float)currentDurability;
            });
        }

        if (!ServerGameManager.TryGetBuffer<ModifyUnitStatBuff_DOTS>(equipmentEntity, out var buffer)) return;

        scaledBonus = CalculateStatBonus(handler, professionLevel);

        for (int i = 0; i < buffer.Length; i++)
        {
            ModifyUnitStatBuff_DOTS statBuff = buffer[i];
            statBuff.Value *= scaledBonus;
            buffer[i] = statBuff;
        }
    }
    static float CalculateStatBonus(IProfession handler, int professionLevel)
    {
        if (handler == null)
            return 0;

        float equipmentMultiplier = handler switch
        {
            BlacksmithingProfession => MAX_WEAPON_BONUS,
            TailoringProfession => MAX_ARMOR_BONUS,
            EnchantingProfession => MAX_MAGIC_BONUS,
            _ => 0
        };

        return 1 + (equipmentMultiplier * (professionLevel / (float)MAX_PROFESSION_LEVEL));
    }
    static float CalculateDurabilityBonus(int professionLevel)
    {
        return 1 + (MAX_DURABILITY_BONUS * (professionLevel / (float)MAX_PROFESSION_LEVEL));
    }
    public static int CalculateProfessionLevelOfEquipmentFromMaxDurability(Entity equipmentEntity)
    {
        Entity prefabEntity = equipmentEntity.GetPrefabEntity();

        float equipmentMaxDurability = equipmentEntity.GetMaxDurability();
        float prefabMaxDurability = prefabEntity.GetMaxDurability();

        float durabilityRatio = equipmentMaxDurability / prefabMaxDurability - 1f;
        int professionLevel = (int)(durabilityRatio * MAX_PROFESSION_LEVEL / MAX_DURABILITY_BONUS);

        return Math.Clamp(professionLevel, 0, MAX_PROFESSION_LEVEL);
    }
}