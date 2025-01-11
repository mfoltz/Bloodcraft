using Bloodcraft.Services;
using ProjectM;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Entities;

namespace Bloodcraft.Systems.Professions;
internal static class EquipmentManager // for potential professions expansion, no idea how to balance this out for existing server settings and still make it worthwhile to players though
{
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    static readonly float _maxDurabilityMultiplier = 1f;
    static readonly float _maxWeaponMultiplier = 0.25f;
    static readonly float _maxArmorMultiplier = 0.25f;
    static readonly float _maxSourceMultiplier = 0.25f;
    public static void ApplyEquipmentStats(ulong steamId, Entity equipmentEntity)
    {
        IProfessionHandler handler = ProfessionHandlerFactory.GetProfessionHandler(equipmentEntity.Read<PrefabGUID>());
        float scaledBonus = 0f;

        if (equipmentEntity.Has<Durability>())
        {
            Durability durability = equipmentEntity.Read<Durability>();
            scaledBonus = CalculateDurabilityBonus(handler, steamId);
            durability.MaxDurability *= scaledBonus;
            durability.Value = durability.MaxDurability;
            equipmentEntity.Write(durability);
        }

        if (!ServerGameManager.TryGetBuffer<ModifyUnitStatBuff_DOTS>(equipmentEntity, out var buffer)) return;
        scaledBonus = CalculateStatBonus(handler, steamId);

        for (int i = 0; i < buffer.Length; i++)
        {
            ModifyUnitStatBuff_DOTS statBuff = buffer[i];
            statBuff.Value *= scaledBonus; // Modify the value accordingly
            buffer[i] = statBuff; // Assign the modified struct back to the buffer
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
            float scaledBonus = 1 + (equipmentMultiplier * (professionLevel / (float)ConfigService.MaxProfessionLevel)); // Scale bonus up to 100%

            return scaledBonus;
        }
        return 0; // Return 0 if no handler is found or other error
    }
    static float CalculateDurabilityBonus(IProfessionHandler handler, ulong steamId)
    {
        if (handler != null)
        {
            int professionLevel = handler.GetProfessionData(steamId).Key;
            float scaledBonus = 1 + (_maxDurabilityMultiplier * (professionLevel / (float)ConfigService.MaxProfessionLevel)); // Scale bonus up to 100%

            return scaledBonus;
        }
        return 0;
    }
}