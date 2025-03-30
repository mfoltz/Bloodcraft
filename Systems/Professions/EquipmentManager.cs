using ProjectM;
using ProjectM.Scripting;
using ProjectM.Shared;
using Unity.Entities;

namespace Bloodcraft.Systems.Professions;
internal static class EquipmentManager
{
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    const float MAX_DURABILITY_MULTIPLIER = 1f;
    const float MAX_WEAPON_MULTIPLIER = 0.1f;
    const float MAX_ARMOR_MULTIPLIER = 0.1f;
    const float MAX_SOURCE_MULTIPLIER = 0.1f;

    const int MAX_PROFESSION_LEVEL = 100;
    public static void ApplyEquipmentStats(ulong steamId, Entity equipmentEntity)
    {
        IProfessionHandler handler = ProfessionHandlerFactory.GetProfessionHandler(equipmentEntity.GetPrefabGuid());

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
            statBuff.Value *= scaledBonus;

            buffer[i] = statBuff;
        }
    }
    public static void ApplyEquipmentStats(int professionLevel, Entity equipmentEntity)
    {
        IProfessionHandler handler = ProfessionHandlerFactory.GetProfessionHandler(equipmentEntity.GetPrefabGuid());
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
            statBuff.Value *= scaledBonus;

            buffer[i] = statBuff;
        }
    }
    static float CalculateStatBonus(IProfessionHandler handler, int professionLevel)
    {
        if (handler == null)
            return 0;

        float equipmentMultiplier = handler switch
        {
            BlacksmithingHandler => MAX_WEAPON_MULTIPLIER,
            TailoringHandler => MAX_ARMOR_MULTIPLIER,
            EnchantingHandler => MAX_SOURCE_MULTIPLIER,
            _ => 0
        };

        return 1 + (equipmentMultiplier * (professionLevel / (float)MAX_PROFESSION_LEVEL));
    }
    static float CalculateDurabilityBonus(int professionLevel)
    {
        return 1 + (MAX_DURABILITY_MULTIPLIER * (professionLevel / (float)MAX_PROFESSION_LEVEL));
    }
    public static int CalculateProfessionLevelOfEquipmentFromMaxDurability(Entity equipmentEntity)
    {
        Entity prefabEntity = equipmentEntity.GetPrefabEntity();

        float equipmentMaxDurability = equipmentEntity.GetMaxDurability();
        float prefabMaxDurability = prefabEntity.GetMaxDurability();

        float durabilityRatio = equipmentMaxDurability / prefabMaxDurability - 1f;
        int professionLevel = (int)(durabilityRatio * MAX_PROFESSION_LEVEL / MAX_DURABILITY_MULTIPLIER);

        return Math.Clamp(professionLevel, 0, MAX_PROFESSION_LEVEL);
    }
}