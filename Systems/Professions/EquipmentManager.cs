using Bloodcraft.Services;
using ProjectM;
using ProjectM.Scripting;
using ProjectM.Shared;
using Unity.Entities;

namespace Bloodcraft.Systems.Professions;
internal static class EquipmentManager
{
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    static readonly float _maxDurabilityMultiplier = 1f;
    static readonly float _maxWeaponMultiplier = 0.1f;
    static readonly float _maxArmorMultiplier = 0.1f;
    static readonly float _maxSourceMultiplier = 0.1f;

    const int MAX_PROFESSION_LEVEL = 100;
    public static void ApplyEquipmentStats(ulong steamId, Entity equipmentEntity)
    {
        IProfessionHandler handler = ProfessionHandlerFactory.GetProfessionHandler(equipmentEntity.GetPrefabGuid());
        float scaledBonus = 0f;

        if (equipmentEntity.Has<Durability>())
        {
            scaledBonus = CalculateDurabilityBonus(handler, steamId);

            equipmentEntity.With((ref Durability durability) =>
            {
                durability.MaxDurability *= scaledBonus;
                durability.Value = durability.MaxDurability;
            });
        }

        if (!ServerGameManager.TryGetBuffer<ModifyUnitStatBuff_DOTS>(equipmentEntity, out var buffer)) return;
        scaledBonus = CalculateStatBonus(handler, steamId);

        for (int i = 0; i < buffer.Length; i++)
        {
            ModifyUnitStatBuff_DOTS statBuff = buffer[i];
            statBuff.Value *= scaledBonus;

            buffer[i] = statBuff;
        }
    }
    static float CalculateStatBonus(IProfessionHandler handler, ulong steamId)
    {
        if (handler != null)
        {
            float equipmentMultiplier = 0;
            string professionName = handler.GetProfessionName();

            if (professionName.Contains("Blacksmithing"))
            {
                equipmentMultiplier = _maxWeaponMultiplier;
            }
            else if (professionName.Contains("Tailoring"))
            {
                equipmentMultiplier = _maxArmorMultiplier;
            }
            else if (professionName.Contains("Enchanting"))
            {
                equipmentMultiplier = _maxSourceMultiplier;
            }

            int professionLevel = handler.GetProfessionData(steamId).Key;
            float scaledBonus = 1 + (equipmentMultiplier * (professionLevel / (float)MAX_PROFESSION_LEVEL));

            return scaledBonus;
        }

        return 0;
    }
    static float CalculateDurabilityBonus(IProfessionHandler handler, ulong steamId)
    {
        if (handler != null)
        {
            int professionLevel = handler.GetProfessionData(steamId).Key;
            float scaledBonus = 1 + (_maxDurabilityMultiplier * (professionLevel / (float)MAX_PROFESSION_LEVEL));

            return scaledBonus;
        }

        return 0;
    }
}