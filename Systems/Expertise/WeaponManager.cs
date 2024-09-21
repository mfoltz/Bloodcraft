using Bloodcraft.Services;
using Bloodcraft.Utilities;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Entities;
using static Bloodcraft.Systems.Expertise.WeaponManager.WeaponStats;

namespace Bloodcraft.Systems.Expertise;
internal static class WeaponManager
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;

    static readonly bool Classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;

    static readonly ComponentType[] UnequipNetworkEventComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<SendEventToUser>()),
        ComponentType.ReadOnly(Il2CppType.Of<NetworkEventType>()),
        ComponentType.ReadOnly(Il2CppType.Of<UnequipItemEvent>())
    ];

    static readonly NetworkEventType UnequipEventType = new()
    {
        IsAdminEvent = false,
        EventId = NetworkEvents.EventId_UnequipItemEvent,
        IsDebugEvent = false
    };

    static readonly ComponentType[] EquipNetworkEventComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<SendEventToUser>()),
        ComponentType.ReadOnly(Il2CppType.Of<NetworkEventType>()),
        ComponentType.ReadOnly(Il2CppType.Of<EquipItemEvent>())
    ];

    static readonly NetworkEventType EquipEventType = new()
    {
        IsAdminEvent = false,
        EventId = NetworkEvents.EventId_EquipItemEvent,
        IsDebugEvent = false
    };
    public static bool ChooseStat(ulong steamId, WeaponType weaponType, WeaponStatType statType)
    {
        if (steamId.TryGetPlayerWeaponStats(out var weaponStats) && weaponStats.TryGetValue(weaponType, out var Stats))
        {
            if (ConfigService.HardSynergies)
            {
                if (!ClassUtilities.HasClass(steamId))
                {
                    return false;
                }

                var classes = steamId.TryGetPlayerClasses(out var classData) ? classData : [];
                var (WeaponStats, _) = classes.First().Value; // get class to check if stat allowed
                List<WeaponStatType> weaponStatTypes = WeaponStats.Select(value => (WeaponStatType)value).ToList();

                if (!weaponStatTypes.Contains(statType)) // hard synergy stat check
                {
                    return false;
                }

                if (Stats.Count >= ConfigService.ExpertiseStatChoices || Stats.Contains(statType))
                {
                    return false; // Only allow configured amount of stats to be chosen and no duplicates
                }

                Stats.Add(statType);
                steamId.SetPlayerWeaponStats(weaponStats);

                return true;
            }
            else
            {
                if (Stats.Count >= ConfigService.ExpertiseStatChoices || Stats.Contains(statType))
                {
                    return false; // Only allow configured amount of stats to be chosen and no duplicates
                }

                Stats.Add(statType);
                steamId.SetPlayerWeaponStats(weaponStats);

                return true;
            }
        }
        return false;
    }
    public static void ResetStats(ulong steamId, WeaponType weaponType)
    {
        if (steamId.TryGetPlayerWeaponStats(out var weaponStats) && weaponStats.TryGetValue(weaponType, out var Stats))
        {
            Stats.Clear();
            steamId.SetPlayerWeaponStats(weaponStats);
        }
    }
    public static void ApplyWeaponStats(ulong steamId, WeaponType weaponType, Entity weaponEntity)
    {
        IExpertiseHandler handler = ExpertiseHandlerFactory.GetExpertiseHandler(weaponType);
        if (steamId.TryGetPlayerWeaponStats(out var weaponStats) && weaponStats.TryGetValue(weaponType, out var bonuses))
        {
            if (!weaponEntity.Has<ModifyUnitStatBuff_DOTS>())
            {
                EntityManager.AddBuffer<ModifyUnitStatBuff_DOTS>(weaponEntity);
            }

            var buffer = weaponEntity.ReadBuffer<ModifyUnitStatBuff_DOTS>();
            foreach (WeaponStatType weaponStatType in bonuses)
            {
                float scaledBonus = CalculateScaledWeaponBonus(handler, steamId, weaponType, weaponStatType);
                bool found = false;

                for (int i = 0; i < buffer.Length; i++)
                {
                    ModifyUnitStatBuff_DOTS statBuff = buffer[i];
                    if (statBuff.StatType.Equals(WeaponStatTypes[weaponStatType])) // Assuming WeaponStatType can be cast to UnitStatType
                    {
                        if (weaponStatType.Equals(WeaponStatType.MovementSpeed))
                        {
                            break;
                        }
                        statBuff.Value += scaledBonus; // Modify the value accordingly
                        buffer[i] = statBuff; // Assign the modified struct back to the buffer
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    UnitStatType statType = WeaponStatTypes[weaponStatType];
                    ModifyUnitStatBuff_DOTS newStatBuff = new()
                    {
                        StatType = statType,
                        ModificationType = ModificationType.AddToBase,
                        Value = scaledBonus,
                        Modifier = 1,
                        IncreaseByStacks = false,
                        ValueByStacks = 0,
                        Priority = 0,
                        Id = ModificationId.Empty
                    };
                    buffer.Add(newStatBuff);
                }
            }
        }
    }
    public static float CalculateScaledWeaponBonus(IExpertiseHandler handler, ulong steamId, WeaponType weaponType, WeaponStatType statType)
    {
        if (handler != null)
        {
            var xpData = handler.GetExpertiseData(steamId);
            float maxBonus = WeaponStatValues[statType];

            if (Classes && steamId.TryGetPlayerClasses(out var classes) && classes.Count != 0)
            {
                var (classWeaponStats, _) = classes.First().Value; // get class to check if stat allowed
                List<WeaponStatType> weaponStatTypes = classWeaponStats.Select(value => (WeaponStatType)value).ToList();

                if (weaponStatTypes.Contains(statType))
                {
                    maxBonus *= ConfigService.StatSynergyMultiplier;
                }
            }

            if (ConfigService.PrestigeSystem && steamId.TryGetPlayerPrestiges(out var prestiges) && prestiges.TryGetValue(WeaponSystem.WeaponPrestigeMap[weaponType], out var PrestigeData))
            {
                float gainFactor = 1 + (ConfigService.PrestigeStatMultiplier * PrestigeData);
                maxBonus *= gainFactor;
            }

            float scaledBonus = maxBonus * ((float)xpData.Key / ConfigService.MaxExpertiseLevel); // Scale bonus up to 99%
            return scaledBonus;
        }
        return 0; // Return 0 if no handler is found or other error
    }
    public static WeaponType GetCurrentWeaponType(Entity character)
    {
        Entity weapon = character.Read<Equipment>().WeaponSlot.SlotEntity._Entity;
        return WeaponSystem.GetWeaponTypeFromSlotEntity(weapon);
    }
    public static void UpdateWeaponStats(Entity character)
    {
        Core.Log.LogInfo("Updating Weapon Stats");
        if (!InventoryUtilities.TryGetInventoryEntity(EntityManager, character, out Entity inventoryEntity)) return;
        
        int userIndex = character.Read<PlayerCharacter>().UserEntity.Read<User>().Index;

        Equipment equipment = character.Read<Equipment>();
        Entity weaponEntity = equipment.WeaponSlot.SlotEntity.GetEntityOnServer();
        int slot = -1;

        EquipItemEvent equipItemEvent = new();
        UnequipItemEvent unequipItemEvent = new();
        SendEventToUser sendEventToUser = new();
        Entity networkEntity = Entity.Null;

        if (!weaponEntity.Exists() && ServerGameManager.TryGetBuffer<InventoryBuffer>(inventoryEntity, out var inventoryBuffer))
        {
            Core.Log.LogInfo("Handling unarmed");

            for (int i = 0; i < inventoryBuffer.Length; i++)
            {
                var item = inventoryBuffer[i];
                if (item.ItemEntity.GetEntityOnServer().TryGetComponent(out Equippable equippable))
                {
                    slot = equippable.EquipBuff.TryGetComponent(out PrefabGUID itemPrefab) && itemPrefab.LookupName().StartsWith("EquipBuff_Weapon") ? i : -1;
                    if (slot != -1) break;
                }
            }

            equipItemEvent = new()
            {
                IsCosmetic = false,
                SlotIndex = slot,
            };

            sendEventToUser = new()
            {
                UserIndex = userIndex
            };

            networkEntity = EntityManager.CreateEntity(EquipNetworkEventComponents);
            networkEntity.Write(sendEventToUser);
            networkEntity.Write(EquipEventType);
            networkEntity.Write(equipItemEvent);

            unequipItemEvent = new()
            {
                EquipmentType = EquipmentType.Weapon,
                ToInventory = inventoryEntity.Read<NetworkId>(),
                ToSlotIndex = slot
            };

            networkEntity = EntityManager.CreateEntity(UnequipNetworkEventComponents);
            networkEntity.Write(sendEventToUser);
            networkEntity.Write(UnequipEventType);
            networkEntity.Write(unequipItemEvent);

            return;
        }
        else if (!InventoryUtilities.TryGetItemSlot(EntityManager, character, weaponEntity, out slot)) return;

        Core.Log.LogInfo("Handling weapon");

        sendEventToUser = new()
        {
            UserIndex = userIndex
        };

        unequipItemEvent = new()
        {
            EquipmentType = EquipmentType.Weapon,
            ToInventory = inventoryEntity.Read<NetworkId>(),
            ToSlotIndex = slot
        };

        networkEntity = EntityManager.CreateEntity(UnequipNetworkEventComponents);
        networkEntity.Write(sendEventToUser);
        networkEntity.Write(UnequipEventType);
        networkEntity.Write(unequipItemEvent);

        equipItemEvent = new()
        {
            IsCosmetic = false,
            SlotIndex = slot,
        };

        networkEntity = EntityManager.CreateEntity(EquipNetworkEventComponents);
        networkEntity.Write(sendEventToUser);
        networkEntity.Write(EquipEventType);
        networkEntity.Write(equipItemEvent);
    }
    public class WeaponStats
    {
        public enum WeaponStatType
        {
            MaxHealth, // 0
            MovementSpeed, // 1
            PrimaryAttackSpeed, // 2
            PhysicalLifeLeech, // 3
            SpellLifeLeech, // 4
            PrimaryLifeLeech, // 5
            PhysicalPower, // 6
            SpellPower, // 7
            PhysicalCritChance, // 8
            PhysicalCritDamage, // 9
            SpellCritChance, // 10
            SpellCritDamage // 11
        }

        public static readonly Dictionary<WeaponStatType, string> WeaponStatFormats = new()
        {
            { WeaponStatType.MaxHealth, "integer" },
            { WeaponStatType.MovementSpeed, "decimal" },
            { WeaponStatType.PrimaryAttackSpeed, "percentage" },
            { WeaponStatType.PhysicalLifeLeech, "percentage" },
            { WeaponStatType.SpellLifeLeech, "percentage" },
            { WeaponStatType.PrimaryLifeLeech, "percentage" },
            { WeaponStatType.PhysicalPower, "integer" },
            { WeaponStatType.SpellPower, "integer" },
            { WeaponStatType.PhysicalCritChance, "percentage" },
            { WeaponStatType.PhysicalCritDamage, "percentage" },
            { WeaponStatType.SpellCritChance, "percentage" },
            { WeaponStatType.SpellCritDamage, "percentage" }
        };

        public static readonly Dictionary<WeaponStatType, UnitStatType> WeaponStatTypes = new()
        {
            { WeaponStatType.MaxHealth, UnitStatType.MaxHealth },
            { WeaponStatType.MovementSpeed, UnitStatType.MovementSpeed },
            { WeaponStatType.PrimaryAttackSpeed, UnitStatType.PrimaryAttackSpeed },
            { WeaponStatType.PhysicalLifeLeech, UnitStatType.PhysicalLifeLeech },
            { WeaponStatType.SpellLifeLeech, UnitStatType.SpellLifeLeech },
            { WeaponStatType.PrimaryLifeLeech, UnitStatType.PrimaryLifeLeech },
            { WeaponStatType.PhysicalPower, UnitStatType.PhysicalPower },
            { WeaponStatType.SpellPower, UnitStatType.SpellPower },
            { WeaponStatType.PhysicalCritChance, UnitStatType.PhysicalCriticalStrikeChance },
            { WeaponStatType.PhysicalCritDamage, UnitStatType.PhysicalCriticalStrikeDamage },
            { WeaponStatType.SpellCritChance, UnitStatType.SpellCriticalStrikeChance },
            { WeaponStatType.SpellCritDamage, UnitStatType.SpellCriticalStrikeDamage },
        };

        public static readonly Dictionary<WeaponStatType, float> WeaponStatValues = new()
        {
            {WeaponStatType.MaxHealth, ConfigService.MaxHealth},
            {WeaponStatType.MovementSpeed, ConfigService.MovementSpeed},
            {WeaponStatType.PrimaryAttackSpeed, ConfigService.PrimaryAttackSpeed},
            {WeaponStatType.PhysicalLifeLeech, ConfigService.PhysicalLifeLeech},
            {WeaponStatType.SpellLifeLeech, ConfigService.SpellLifeLeech},
            {WeaponStatType.PrimaryLifeLeech, ConfigService.PrimaryLifeLeech},
            {WeaponStatType.PhysicalPower, ConfigService.PhysicalPower},
            {WeaponStatType.SpellPower, ConfigService.SpellPower},
            {WeaponStatType.PhysicalCritChance, ConfigService.PhysicalCritChance},
            {WeaponStatType.PhysicalCritDamage, ConfigService.PhysicalCritDamage},
            {WeaponStatType.SpellCritChance, ConfigService.SpellCritChance},
            {WeaponStatType.SpellCritDamage, ConfigService.SpellCritDamage}
        };
    }
}
