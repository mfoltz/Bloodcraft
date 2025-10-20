using Bloodcraft.Interfaces;
using Bloodcraft.Patches;
using Bloodcraft.Services;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using System.Collections;
using System.Text.RegularExpressions;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using VampireCommandFramework;
using static Bloodcraft.Services.PlayerService;
using static Bloodcraft.Systems.Expertise.WeaponManager.WeaponStats;
using static Bloodcraft.Systems.Legacies.BloodManager.BloodStats;
using static Bloodcraft.Systems.Leveling.ClassManager;
using static Bloodcraft.Systems.Leveling.LevelingSystem;
using static Bloodcraft.Utilities.EntityQueries;
using static Bloodcraft.Utilities.Misc.PlayerBoolsManager;

namespace Bloodcraft.Utilities;
internal static class Classes
{
    static EntityManager EntityManager => Core.EntityManager;

    static EntityManager _entityManagerRef = Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static PrefabCollectionSystem PrefabCollectionSystem => SystemService.PrefabCollectionSystem;
    static ActivateVBloodAbilitySystem ActivateVBloodAbilitySystem => SystemService.ActivateVBloodAbilitySystem;
    static ReplaceAbilityOnSlotSystem ReplaceAbilityOnSlotSystem => SystemService.ReplaceAbilityOnSlotSystem;

    static readonly WaitForSeconds _longDelay = new(10f);
    static readonly Regex _classNameRegex = new("(?<!^)([A-Z])");

    static readonly PrefabGUID _vBloodAbilityBuff = Buffs.VBloodAbilityReplaceBuff;

    const string NO_NAME = "No Name";
    const string PRIMARY_ATTACK = "Primary Attack";
    const int PASSIVE_BATCH = 3;
    const int SYNERGY_BATCH = 6;
    const int SPELL_BATCH = 4;
    static NativeParallelHashMap<PrefabGUID, ItemData> ItemLookup => SystemService.GameDataSystem.ItemHashLookupMap;
    static PrefabLookupMap _prefabLookupMap = PrefabCollectionSystem._PrefabLookupMap;

    static readonly Dictionary<PrefabGUID, List<Entity>> _abilityJewelMap = [];

    static QueryDesc _jewelQueryDesc;

    static readonly ComponentType[] _jewelComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<PrefabGUID>()),
        ComponentType.ReadOnly(Il2CppType.Of<JewelInstance>()),
        ComponentType.ReadOnly(Il2CppType.Of<JewelLevelSource>()),
        ComponentType.ReadOnly(Il2CppType.Of<ItemData>()),
        ComponentType.ReadOnly(Il2CppType.Of<InventoryItem>()),
        ComponentType.ReadOnly(Il2CppType.Of<SpellModSetComponent>())
    ];

    static readonly int[] _typeIndices = [0, 1];

    static readonly Dictionary<PlayerClass, string> _classColorMap = new()
    {
        { PlayerClass.ShadowBlade, "#A020F0" },
        { PlayerClass.DemonHunter, "#FFD700" },
        { PlayerClass.BloodKnight, "#FF0000" },
        { PlayerClass.ArcaneSorcerer, "#008080" },
        { PlayerClass.VampireLord, "#00FFFF" },
        { PlayerClass.DeathMage, "#00FF00" }
    };

    public static readonly Dictionary<PlayerClass, List<PrefabGUID>> ClassShiftAbilities = new()
    {
        [PlayerClass.BloodKnight] =
        [
            new(0),
            new(0),
            new(0),
            new(0),
            new(0),
            new(0),
            new(0),
            new(0),
            new(0),
            new(0)
        ],
        [PlayerClass.VampireLord] =
        [
            new(0),
            new(0),
            new(0),
            new(0),
            new(0),
            new(0),
            new(0),
            new(0),
            new(0),
            new(0)
        ],
        [PlayerClass.DemonHunter] =
        [
            new(0),
            new(0),
            new(0),
            new(0),
            new(0),
            new(0),
            new(0),
            new(0),
            new(0),
            new(0)
        ],
        [PlayerClass.ShadowBlade] =
        [
            new(0),
            new(0),
            new(0),
            new(0),
            new(0),
            new(0),
            new(0),
            new(0),
            new(0),
            new(0)
        ],
        [PlayerClass.ArcaneSorcerer] =
        [
            new(0),
            new(0),
            new(0),
            new(0),
            new(0),
            new(0),
            new(0),
            new(0),
            new(0),
            new(0)
        ],
        [PlayerClass.DeathMage] =
        [
            new(0),
            new(0),
            new(0),
            new(0),
            new(0),
            new(0),
            new(0),
            new(0),
            new(0),
            new(0)
        ]
    };

    // Class Abilities
    // AB_Vampire_BloodKnight_HighKick_Abilitygroup PrefabGuid(-1638937811)

    // this does not really pan out anymore but can still use stats as template
    // guess can try to match current default power levels more or less, also need to consider how to remove all of the old buffs gracefully probably by filtering for some uniqueness about their permanence
    public static readonly Dictionary<PrefabGUID, Dictionary<UnitStatType, float>> BuffStatBonuses = new()
    {
        // Shared - Warrior (BloodKnight & VampireLord)
        [new PrefabGUID(-500035095)] = new() // SetBonus_SpellPower_T06
        {
            { UnitStatType.MaxHealth, 200f },
        },
        [new PrefabGUID(-567664068)] = new() // SetBonus_Speed_Minor_Buff_02
        {
            { UnitStatType.PhysicalPower, 10f },
        },
        [new PrefabGUID(1178148717)] = new() // SetBonus_MaxHealth_PhysPower_T08
        {
            { UnitStatType.HealthRecovery, 0.05f },
        },

        // Unique - BloodKnight
        [new PrefabGUID(-753729496)] = new() // SetBonus_AttackSpeed_Minor_Buff_01
        {
            { UnitStatType.PhysicalResistance, 0.25f },
        },
        [new PrefabGUID(1966156848)] = new() // SetBonus_MovementSpeed_T06
        {
            { UnitStatType.PrimaryLifeLeech, 0.1f },
        },
        [new PrefabGUID(-1281560674)] = new() // SetBonus_Speed_Minor_Buff_01
        {
            { UnitStatType.HealingReceived, 0.5f },
        },

        // Unique - VampireLord
        [new PrefabGUID(-965685546)] = new() // SetBonus_Damage_Minor_Buff_02
        {
            { UnitStatType.SpellResistance, 0.5f },
        },
        [new PrefabGUID(249601863)] = new() // SetBonus_AttackSpeed_T04
        {
            { UnitStatType.PhysicalLifeLeech, 0.25f },
        },
        [new PrefabGUID(-1240045321)] = new() // SetBonus_MovementSpeed_T04
        {
            { UnitStatType.WeaponCooldownRecoveryRate, 0.2f },
        },

        // Shared - Rogue (DemonHunter & ShadowBlade)
        [new PrefabGUID(-1161614593)] = new() // SetBonus_PhysicalCritChance_T04
        {
            { UnitStatType.PrimaryAttackSpeed, 0.25f }
        },
        [new PrefabGUID(-2026669113)] = new() // SetBonus_AttackSpeed_Minor_Buff_02
        {
            { UnitStatType.MovementSpeed, 0.5f }
        },
        [new PrefabGUID(495242428)] = new() // SetBonus_Damage_Minor_Buff_01
        {
            { UnitStatType.PhysicalCriticalStrikeChance, 0.2f },
        },

        // Unique - DemonHunter
        [new PrefabGUID(-1630759636)] = new() // SetBonus_MovementSpeed_PhysCritChance_T08
        {
            { UnitStatType.PrimaryCooldownModifier, 0.2f },
        },
        [new PrefabGUID(-692773400)] = new() // SetBonus_SpellPower_SpellLeech_T08
        {
            { UnitStatType.PhysicalCriticalStrikeDamage, 0.5f },
        },
        [new PrefabGUID(803329072)] = new() // SetBonus_CCReduction_T06
        {
            { UnitStatType.PrimaryLifeLeech, 0.15f },
        },

        // Unique - ShadowBlade
        [new PrefabGUID(-1100642493)] = new() // SetBonus_Speed_02
        {
            { UnitStatType.CooldownRecoveryRate, 0.2f },
        },
        [new PrefabGUID(505940050)] = new() // SetBonus_AttackSpeed_PhysPower_T08
        {
            // { UnitStatType.AttackSpeed, 0.2f },
        },
        [new PrefabGUID(-104461547)] = new() // SetBonus_PhysicalCritChance_T06
        {
            { UnitStatType.PhysicalResistance, 0.2f },
        },

        // Shared - Mage (ArcaneSorcerer & DeathMage)
        [new PrefabGUID(1777596670)] = new() // SetBonus_SpellPower_T04
        {
            { UnitStatType.SpellPower, 10f }
        },
        [new PrefabGUID(2070760442)] = new() // SetBonus_HealingReceived_T06
        {
            { UnitStatType.SpellLifeLeech, 0.1f },
        },
        [new PrefabGUID(32536495)] = new() // SetBonus_HealingReceived_T04
        {
            { UnitStatType.SpellCriticalStrikeChance, 0.15f }
        },

        // Unique - ArcaneSorcerer
        [new PrefabGUID(-753729496)] = new() // SetBonus_AttackSpeed_Minor_Buff_01
        {
            // { UnitStatType.AttackSpeed, 0.1f },
        },
        [new PrefabGUID(-567664068)] = new() // SetBonus_Speed_Minor_Buff_02
        {
            { UnitStatType.SpellCriticalStrikeDamage, 0.5f },
        },
        [new PrefabGUID(-1630759636)] = new() // SetBonus_MovementSpeed_PhysCritChance_T08
        {
            { UnitStatType.UltimateCooldownRecoveryRate, 0.2f },
        },

        // Unique - DeathMage
        [new PrefabGUID(-692773400)] = new() // SetBonus_SpellPower_SpellLeech_T08
        {
            { UnitStatType.MinionDamage, 0.25f },
        },
        [new PrefabGUID(908755409)] = new() // SetBonus_AttackSpeed_T06
        {
            // { UnitStatType.ShieldAbsorb, 0.5f }, // onhit synergy?
        },
        [new PrefabGUID(-176045156)] = new() // SetBonus_DamageReduction_T08
        {
            { UnitStatType.HealingReceived, 0.25f },
        }
    };

    public static readonly Dictionary<PlayerClass, (string, string)> ClassWeaponBloodMap = new()
    {
        { PlayerClass.BloodKnight, (ConfigService.BloodKnightWeaponSynergies, ConfigService.BloodKnightBloodSynergies) },
        { PlayerClass.DemonHunter, (ConfigService.DemonHunterWeaponSynergies, ConfigService.DemonHunterBloodSynergies) },
        { PlayerClass.VampireLord, (ConfigService.VampireLordWeaponSynergies, ConfigService.VampireLordBloodSynergies) },
        { PlayerClass.ShadowBlade, (ConfigService.ShadowBladeWeaponSynergies, ConfigService.ShadowBladeBloodSynergies) },
        { PlayerClass.ArcaneSorcerer, (ConfigService.ArcaneSorcererWeaponSynergies, ConfigService.ArcaneSorcererBloodSynergies) },
        { PlayerClass.DeathMage, (ConfigService.DeathMageWeaponSynergies, ConfigService.DeathMageBloodSynergies) }
    };

    public static readonly Dictionary<PlayerClass, (List<int>, List<int>)> ClassWeaponBloodEnumMap = new()
    {
        { PlayerClass.BloodKnight, (Configuration.ParseEnumsFromString<WeaponStatType>(ConfigService.BloodKnightWeaponSynergies).Select(e => (int)e).ToList(), Configuration.ParseEnumsFromString<BloodStatType>(ConfigService.BloodKnightBloodSynergies).Select(e => (int)e).ToList()) },
        { PlayerClass.DemonHunter, (Configuration.ParseEnumsFromString<WeaponStatType>(ConfigService.DemonHunterWeaponSynergies).Select(e => (int)e).ToList(), Configuration.ParseEnumsFromString<BloodStatType>(ConfigService.DemonHunterBloodSynergies).Select(e => (int)e).ToList()) },
        { PlayerClass.VampireLord, (Configuration.ParseEnumsFromString<WeaponStatType>(ConfigService.VampireLordWeaponSynergies).Select(e => (int)e).ToList(), Configuration.ParseEnumsFromString<BloodStatType>(ConfigService.VampireLordBloodSynergies).Select(e => (int)e).ToList()) },
        { PlayerClass.ShadowBlade, (Configuration.ParseEnumsFromString<WeaponStatType>(ConfigService.ShadowBladeWeaponSynergies).Select(e => (int)e).ToList(), Configuration.ParseEnumsFromString<BloodStatType>(ConfigService.ShadowBladeBloodSynergies).Select(e => (int)e).ToList()) },
        { PlayerClass.ArcaneSorcerer, (Configuration.ParseEnumsFromString<WeaponStatType>(ConfigService.ArcaneSorcererWeaponSynergies).Select(e => (int)e).ToList(), Configuration.ParseEnumsFromString<BloodStatType>(ConfigService.ArcaneSorcererBloodSynergies).Select(e => (int)e).ToList()) },
        { PlayerClass.DeathMage, (Configuration.ParseEnumsFromString<WeaponStatType>(ConfigService.DeathMageWeaponSynergies).Select(e => (int)e).ToList(), Configuration.ParseEnumsFromString<BloodStatType>(ConfigService.DeathMageBloodSynergies).Select(e => (int)e).ToList()) }
    };

    /*
    public static readonly Dictionary<PlayerClass, string> ClassBuffMap = new()
    {
        { PlayerClass.BloodKnight, ConfigService.BloodKnightBuffs },
        { PlayerClass.DemonHunter, ConfigService.DemonHunterBuffs },
        { PlayerClass.VampireLord, ConfigService.VampireLordBuffs },
        { PlayerClass.ShadowBlade, ConfigService.ShadowBladeBuffs },
        { PlayerClass.ArcaneSorcerer, ConfigService.ArcaneSorcererBuffs },
        { PlayerClass.DeathMage, ConfigService.DeathMageBuffs }
    };
    */

    public static readonly Dictionary<PlayerClass, string> ClassSpellsMap = new()
    {
        { PlayerClass.BloodKnight, ConfigService.BloodKnightSpells },
        { PlayerClass.DemonHunter, ConfigService.DemonHunterSpells },
        { PlayerClass.VampireLord, ConfigService.VampireLordSpells },
        { PlayerClass.ShadowBlade, ConfigService.ShadowBladeSpells },
        { PlayerClass.ArcaneSorcerer, ConfigService.ArcaneSorcererSpells },
        { PlayerClass.DeathMage, ConfigService.DeathMageSpells }
    };
    public static List<PrefabGUID> GetClassBuffs(ulong steamId)
    {
        if (steamId.HasClass(out PlayerClass? playerClass) && playerClass.HasValue)
        {
            return UpdateBuffsBufferDestroyPatch.ClassBuffsOrdered.TryGetValue(playerClass.Value, out var classBuffs) ?
                classBuffs : [];
        }

        return [];
    }
    public static bool HandleClassChangeItem(ChatCommandContext ctx)
    {
        PrefabGUID item = new(ConfigService.ChangeClassItem);
        int quantity = ConfigService.ChangeClassQuantity;

        if (!InventoryUtilities.TryGetInventoryEntity(EntityManager, ctx.User.LocalCharacter._Entity, out var inventoryEntity) ||
            ServerGameManager.GetInventoryItemCount(inventoryEntity, item) < quantity)
        {
            LocalizationService.HandleReply(ctx, $"You do not have enough of the required item to change classes (<color=#ffd9eb>{item.GetLocalizedName()}</color>x<color=white>{quantity}</color>)");
            return false;
        }

        if (!ServerGameManager.TryRemoveInventoryItem(inventoryEntity, item, quantity))
        {
            LocalizationService.HandleReply(ctx, $"Failed to remove enough of the item required (<color=#ffd9eb>{item.GetLocalizedName()}</color>x<color=white>{quantity}</color>)");
            return false;
        }

        return true;
    }
    public static bool HasClass(this ulong steamId, out PlayerClass? playerClass)
    {
        playerClass = null;

        if (steamId.TryGetPlayerClass(out PlayerClass playerClassType))
        {
            playerClass = playerClassType;
            return true;
        }

        return false;
    }
    public static void RemoveClassBuffs(Entity playerCharacter, List<PrefabGUID> classBuffs)
    {
        foreach (PrefabGUID buff in classBuffs)
        {
            // Core.Log.LogInfo($"Removing buff for {playerCharacter.GetSteamId()} - {buff.GetPrefabName()}");
            playerCharacter.TryRemoveBuff(buffPrefabGuid: buff);
        }
    }
    public static void RemoveAllClassBuffs(Entity playerCharacter)
    {
        // Core.Log.LogInfo("Removing all class buffs...");

        foreach (List<PrefabGUID> classBuffs in UpdateBuffsBufferDestroyPatch.ClassBuffsOrdered.Values)
        {
            foreach (PrefabGUID buff in classBuffs)
            {
                // Core.Log.LogInfo($"Removing buff - {buff.GetPrefabName()}");
                playerCharacter.TryRemoveBuff(buffPrefabGuid: buff);
            }
        }
    }
    public static void ReplyClassBuffs(ChatCommandContext ctx, PlayerClass playerClass)
    {
        List<PrefabGUID> passiveBuffs = UpdateBuffsBufferDestroyPatch.ClassBuffsOrdered[playerClass];
        var prefabGuidEntityMap = PrefabCollectionSystem._PrefabGuidToEntityMap;

        if (passiveBuffs.Count == 0)
        {
            LocalizationService.HandleReply(ctx, $"{FormatClassName(playerClass)} passives not found!");
            return;
        }

        int step = ConfigService.MaxLevel / passiveBuffs.Count;

        var classBuffs = passiveBuffs.Select((buff, index) =>
        {
            Entity prefabEntity = prefabGuidEntityMap[buff];
            string prefabName = buff.GetPrefabName();

            int level = (index + 1) * step;
            int prefabIndex = prefabName.IndexOf("Prefab");

            if (prefabIndex != -1)
            {
                prefabName = prefabName[..prefabIndex].TrimEnd();
            }

            return $"<color=yellow>{index + 1}</color>| {(prefabEntity.Has<ModifyUnitStatBuff_DOTS>() ? FormatModifyUnitStatBuffer(prefabEntity) : prefabName)} at level <color=green>{level}</color>";
        }).ToList();

        LocalizationService.HandleReply(ctx, $"{FormatClassName(playerClass)} passives:");

        foreach (var batch in classBuffs.Batch(PASSIVE_BATCH))
        {
            string replyMessage = string.Join(", ", batch);
            LocalizationService.HandleReply(ctx, $"{replyMessage}");
        }
    }

    public static void ReplyClassSynergies(ChatCommandContext ctx, PlayerClass playerClass)
    {
        var hasWeaponStats = ClassWeaponStatSynergies.TryGetValue(playerClass, out var weaponStats);
        var hasBloodStats = ClassBloodStatSynergies.TryGetValue(playerClass, out var bloodStats);

        if (!hasWeaponStats && !hasBloodStats)
        {
            LocalizationService.HandleReply(ctx, $"Couldn't find stat synergies for {FormatClassName(playerClass)}...");
            return;
        }

        var allStats = new List<string>();

        if (hasWeaponStats && weaponStats is { Count: > 0 })
        {
            allStats.AddRange(weaponStats.Select(stat => $"<color=white>{stat}</color> (<color=#00FFFF>Weapon</color>)"));
        }

        if (hasBloodStats && bloodStats is { Count: > 0 })
        {
            allStats.AddRange(bloodStats.Select(stat => $"<color=white>{stat}</color> (<color=red>Blood</color>)"));
        }

        if (allStats.Count == 0)
        {
            LocalizationService.HandleReply(ctx, $"No stat synergies found for {FormatClassName(playerClass)}.");
            return;
        }

        LocalizationService.HandleReply(ctx, $"{FormatClassName(playerClass)} stat synergies [x<color=white>{ConfigService.SynergyMultiplier}</color>]:");

        foreach (var batch in allStats.Batch(SYNERGY_BATCH))
        {
            string replyMessage = string.Join(", ", batch);
            LocalizationService.HandleReply(ctx, $"{replyMessage}");
        }
    }

    /*
    public static void ReplyClassSynergies(ChatCommandContext ctx, PlayerClass playerClass)
    {
        if (ClassWeaponBloodMap.TryGetValue(playerClass, out var weaponBloodStats))
        {
            var weaponStats = weaponBloodStats.Item1.Split(',').Select(v => ((WeaponStatType)int.Parse(v)).ToString()).ToList();
            var bloodStats = weaponBloodStats.Item2.Split(',').Select(v => ((BloodStatType)int.Parse(v)).ToString()).ToList();

            if (weaponStats.Count == 0 && bloodStats.Count == 0)
            {
                LocalizationService.HandleReply(ctx, $"No stat synergies found for {FormatClassName(playerClass)}.");
                return;
            }

            var allStats = new List<string>();
            allStats.AddRange(weaponStats.Select(stat => $"<color=white>{stat}</color> (<color=#00FFFF>Weapon</color>)"));
            allStats.AddRange(bloodStats.Select(stat => $"<color=white>{stat}</color> (<color=red>Blood</color>)"));

            LocalizationService.HandleReply(ctx, $"{FormatClassName(playerClass)} stat synergies[x<color=white>{ConfigService.SynergyMultiplier}</color>]:");

            for (int i = 0; i < allStats.Count; i += 6)
            {
                var batch = allStats.Skip(i).Take(6);
                string replyMessage = string.Join(", ", batch);
                LocalizationService.HandleReply(ctx, $"{replyMessage}");
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, $"Couldn't find stat synergies for {FormatClassName(playerClass)}...");
        }
    }
    */
    public static void ReplyClassSpells(ChatCommandContext ctx, PlayerClass playerClass)
    {
        List<int> spells = Configuration.ParseIntegersFromString(ClassSpellsMap[playerClass]);

        if (spells.Count == 0)
        {
            LocalizationService.HandleReply(ctx, $"{FormatClassName(playerClass)} has no spells configured...");
            return;
        }

        var classSpells = spells.Select((spell, index) =>
        {
            PrefabGUID spellPrefabGuid = new(spell);
            string spellName = GetClassSpellName(spellPrefabGuid);

            return $"<color=yellow>{index + 1}</color>| <color=white>{spellName}</color>";
        }).ToList();

        if (!ConfigService.DefaultClassSpell.Equals(0))
        {
            PrefabGUID spellPrefabGuid = new(ConfigService.DefaultClassSpell);
            string spellName = GetClassSpellName(spellPrefabGuid);

            classSpells.Insert(0, $"<color=yellow>{0}</color>| <color=white>{spellName}</color>");
        }

        LocalizationService.HandleReply(ctx, $"{FormatClassName(playerClass)} spells:");

        foreach (var batch in classSpells.Batch(SPELL_BATCH))
        {
            string replyMessage = string.Join(", ", batch);
            LocalizationService.HandleReply(ctx, $"{replyMessage}");
        }
    }
    public static bool TryParseClass(string classType, out PlayerClass parsedClassType)
    {
        if (Enum.TryParse(classType, true, out parsedClassType))
        {
            return true;
        }

        parsedClassType = Enum.GetValues(typeof(PlayerClass))
                              .Cast<PlayerClass>()
                              .FirstOrDefault(pc => pc.ToString().Contains(classType, StringComparison.CurrentCultureIgnoreCase));

        if (!parsedClassType.Equals(default(PlayerClass)))
        {
            return true;
        }

        parsedClassType = default;
        return false;
    }
    public static void UpdatePlayerClass(Entity character, PlayerClass parsedClassType, ulong steamId)
    {
        if (steamId.HasClass(out PlayerClass? playerClass) && playerClass.HasValue)
        {
            // List<PrefabGUID> classBuffs = GetClassBuffs(steamId);
            // RemoveClassBuffs(character, classBuffs);
        }

        steamId.SetPlayerClass(parsedClassType);
        Buffs.RefreshStats(character);
        // ApplyClassBuffs(character, steamId);
    }
    public static void RemoveShift(Entity character)
    {
        Entity abilityGroup = ServerGameManager.GetAbilityGroup(character, 3);

        if (abilityGroup.Exists() && abilityGroup.Has<VBloodAbilityData>())
        {
            Entity buffEntity = Entity.Null;

            bool tryEquip = false;
            Entity equippedJewelEntity = Entity.Null;

            if (ServerGameManager.TryGetBuffer<VBloodAbilityBuffEntry>(character, out var firstBuffer))
            {
                PrefabGUID oldAbility = abilityGroup.Read<PrefabGUID>();
                int index = -1;

                for (int i = 0; i < firstBuffer.Length; i++)
                {
                    VBloodAbilityBuffEntry vBloodAbilityBuffEntry = firstBuffer[i];
                    if (vBloodAbilityBuffEntry.SlotId == 3)
                    {
                        buffEntity = vBloodAbilityBuffEntry.ActiveBuff;
                        index = i;
                        break;
                    }
                }

                if (InventoryUtilities.TryGetInventoryEntity(EntityManager, character, out Entity inventoryEntity) && _abilityJewelMap.TryGetValue(oldAbility, out List<Entity> jewelEntities))
                {
                    foreach (Entity jewel in jewelEntities)
                    {
                        if (JewelEquipUtilitiesServer.TryGetEquippedJewel(EntityManager, character, jewel, out equippedJewelEntity) && equippedJewelEntity.Exists())
                        {
                            break;
                        }
                    }

                    var _itemLookup = ItemLookup;

                    if (JewelEquipUtilitiesServer.TryUnequipJewel(EntityManager, ref _itemLookup, inventoryEntity, abilityGroup))
                    {
                        tryEquip = true;
                    }
                }

                if ((index >= 0 && index < firstBuffer.Length) && buffEntity.Exists())
                {
                    firstBuffer.RemoveAt(index);
                    DestroyUtility.Destroy(EntityManager, buffEntity, DestroyDebugReason.TryRemoveBuff);
                }
            }

            ReplaceAbilityOnSlotSystem.OnUpdate();

            if (tryEquip && InventoryUtilities.TryGetItemSlot(EntityManager, character, equippedJewelEntity, out int slot))
            {
                JewelEquipUtilitiesServer.TryEquipJewel(ref _entityManagerRef, ref _prefabLookupMap, character, slot);
            }

            RemoveNPCSpell(character);
        }
        else
        {
            RemoveNPCSpell(character);
        }
    }
    public static void UpdateShift(ChatCommandContext ctx, Entity character, PrefabGUID spellPrefabGUID)
    {
        if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(spellPrefabGUID, out Entity ability) && ability.TryGetComponent(out VBloodAbilityData vBloodAbilityData))
        {
            bool tryEquip = false;

            Entity inventoryEntity = Entity.Null;
            Entity equippedJewelEntity = Entity.Null;
            Entity abilityGroup = ServerGameManager.GetAbilityGroup(character, 3);
            string spellName = GetClassSpellName(spellPrefabGUID);

            if (abilityGroup.Exists() && ServerGameManager.TryGetBuffer<VBloodAbilityBuffEntry>(character, out var firstBuffer))
            {
                Entity buffEntity = Entity.Null;
                PrefabGUID oldAbility = abilityGroup.Read<PrefabGUID>();
                int index = -1;

                if (spellPrefabGUID.Equals(oldAbility))
                {
                    LocalizationService.HandleReply(ctx, $"Shift spell: <color=#CBC3E3>{spellName}</color>");
                    return;
                }
                for (int i = 0; i < firstBuffer.Length; i++)
                {
                    VBloodAbilityBuffEntry vBloodAbilityBuffEntry = firstBuffer[i];
                    if (vBloodAbilityBuffEntry.ActiveAbility.Equals(oldAbility) && vBloodAbilityBuffEntry.SlotId == 3)
                    {
                        buffEntity = vBloodAbilityBuffEntry.ActiveBuff;
                        index = i;
                        break;
                    }
                }

                if (InventoryUtilities.TryGetInventoryEntity(EntityManager, character, out inventoryEntity) && _abilityJewelMap.TryGetValue(oldAbility, out List<Entity> jewelEntities))
                {
                    foreach (Entity jewel in jewelEntities)
                    {
                        if (JewelEquipUtilitiesServer.TryGetEquippedJewel(EntityManager, character, jewel, out equippedJewelEntity) && equippedJewelEntity.Exists())
                        {
                            break;
                        }
                    }

                    var _itemLookup = ItemLookup;

                    if (JewelEquipUtilitiesServer.TryUnequipJewel(EntityManager, ref _itemLookup, inventoryEntity, abilityGroup))
                    {
                        tryEquip = true;
                    }
                }

                if (index >= 0 && index < firstBuffer.Length && buffEntity.Exists())
                {
                    firstBuffer.RemoveAt(index);
                    DestroyUtility.Destroy(EntityManager, buffEntity, DestroyDebugReason.TryRemoveBuff);
                }
            }

            // cache that, geez
            VBloodAbilityUtilities.InstantiateBuff(EntityManager, ActivateVBloodAbilitySystem._BuffSpawnerSystemData, character, PrefabCollectionSystem._PrefabGuidToEntityMap[_vBloodAbilityBuff], spellPrefabGUID, 3);

            if (ServerGameManager.TryGetBuffer<VBloodAbilityBuffEntry>(character, out var secondBuffer))
            {
                foreach (VBloodAbilityBuffEntry abilityEntry in secondBuffer)
                {
                    if (abilityEntry.ActiveAbility.Equals(spellPrefabGUID))
                    {
                        abilityEntry.ActiveBuff.With((ref VBloodAbilityReplaceBuff vBloodAbilityReplaceBuff)
                            => vBloodAbilityReplaceBuff.AbilityType = vBloodAbilityData.AbilityType);

                        break;
                    }
                }
            }

            ReplaceAbilityOnSlotSystem.OnUpdate();

            if (tryEquip && InventoryUtilities.TryGetItemSlot(EntityManager, character, equippedJewelEntity, out int slot))
            {
                JewelEquipUtilitiesServer.TryEquipJewel(ref _entityManagerRef, ref _prefabLookupMap, character, slot);
            }

            LocalizationService.HandleReply(ctx, $"Shift spell: <color=#CBC3E3>{spellName}</color>");
        }
        else if (spellPrefabGUID.HasValue())
        {
            Entity inventoryEntity = Entity.Null;
            Entity equippedJewelEntity = Entity.Null;
            Entity abilityGroup = ServerGameManager.GetAbilityGroup(character, 3);
            string spellName = GetClassSpellName(spellPrefabGUID);

            if (abilityGroup.Exists() && ServerGameManager.TryGetBuffer<VBloodAbilityBuffEntry>(character, out var firstBuffer)) // take care of spell if was normal
            {
                bool tryEquip = false;

                Entity buffEntity = Entity.Null;
                PrefabGUID oldAbility = abilityGroup.Read<PrefabGUID>();
                int index = -1;

                if (spellPrefabGUID.Equals(oldAbility))
                {
                    LocalizationService.HandleReply(ctx, $"Shift spell: <color=#CBC3E3>{spellName}</color>");
                    return;
                }
                for (int i = 0; i < firstBuffer.Length; i++)
                {
                    VBloodAbilityBuffEntry vBloodAbilityBuffEntry = firstBuffer[i];
                    if (vBloodAbilityBuffEntry.ActiveAbility.Equals(oldAbility) && vBloodAbilityBuffEntry.SlotId == 3)
                    {
                        buffEntity = vBloodAbilityBuffEntry.ActiveBuff;
                        index = i;
                        break;
                    }
                }

                if (InventoryUtilities.TryGetInventoryEntity(EntityManager, character, out inventoryEntity) && _abilityJewelMap.TryGetValue(oldAbility, out List<Entity> jewelEntities))
                {
                    foreach (Entity jewel in jewelEntities)
                    {
                        if (JewelEquipUtilitiesServer.TryGetEquippedJewel(EntityManager, character, jewel, out equippedJewelEntity) && equippedJewelEntity.Exists())
                        {
                            break;
                        }
                    }

                    var _itemLookup = ItemLookup;

                    if (JewelEquipUtilitiesServer.TryUnequipJewel(EntityManager, ref _itemLookup, inventoryEntity, abilityGroup))
                    {
                        tryEquip = true;
                    }
                }

                if (index >= 0 && index < firstBuffer.Length && buffEntity.Exists())
                {
                    firstBuffer.RemoveAt(index);
                    DestroyUtility.Destroy(EntityManager, buffEntity, DestroyDebugReason.TryRemoveBuff);
                }

                HandleNPCSpell(character, spellPrefabGUID);

                if (tryEquip && InventoryUtilities.TryGetItemSlot(EntityManager, character, equippedJewelEntity, out int slot))
                {
                    JewelEquipUtilitiesServer.TryEquipJewel(ref _entityManagerRef, ref _prefabLookupMap, character, slot);
                }

                LocalizationService.HandleReply(ctx, $"Shift spell: <color=#CBC3E3>{spellName}</color>");
            }
            else
            {
                HandleNPCSpell(character, spellPrefabGUID);
                LocalizationService.HandleReply(ctx, $"Shift spell: <color=#CBC3E3>{spellName}</color>");
            }
        }
    }
    static void RemoveNPCSpell(Entity character)
    {
        Entity buffEntity = Entity.Null;
        var buffer = character.ReadBuffer<BuffBuffer>();

        for (int i = 0; i < buffer.Length; i++)
        {
            BuffBuffer item = buffer[i];
            if (item.PrefabGuid.GetPrefabName().StartsWith("EquipBuff_Weapon"))
            {
                buffEntity = item.Entity;
                break;
            }
        }

        var replaceBuffer = buffEntity.ReadBuffer<ReplaceAbilityOnSlotBuff>();
        int toRemove = -1;

        for (int i = 0; i < replaceBuffer.Length; i++)
        {
            ReplaceAbilityOnSlotBuff item = replaceBuffer[i];
            if (item.Slot == 3)
            {
                toRemove = i;
                break;
            }
        }

        if (toRemove >= 0 && toRemove < replaceBuffer.Length) replaceBuffer.RemoveAt(toRemove);

        ServerGameManager.ModifyAbilityGroupOnSlot(buffEntity, character, 3, PrefabGUID.Empty);
    }
    static void HandleNPCSpell(Entity character, PrefabGUID spellPrefabGUID)
    {
        Entity buffEntity = Entity.Null;
        var buffer = character.ReadBuffer<BuffBuffer>();

        for (int i = 0; i < buffer.Length; i++)
        {
            BuffBuffer item = buffer[i];
            if (item.PrefabGuid.GetPrefabName().StartsWith("EquipBuff_Weapon"))
            {
                buffEntity = item.Entity;
                break;
            }
        }

        var replaceBuffer = buffEntity.ReadBuffer<ReplaceAbilityOnSlotBuff>();
        int toRemove = -1;

        for (int i = 0; i < replaceBuffer.Length; i++)
        {
            ReplaceAbilityOnSlotBuff item = replaceBuffer[i];
            if (item.Slot == 3)
            {
                toRemove = i;
                break;
            }
        }

        if (toRemove >= 0 && toRemove < replaceBuffer.Length) replaceBuffer.RemoveAt(toRemove);

        ReplaceAbilityOnSlotBuff buff = new()
        {
            Slot = 3,
            NewGroupId = spellPrefabGUID,
            CopyCooldown = true,
            Priority = 0,
        };

        replaceBuffer.Add(buff);
        ServerGameManager.ModifyAbilityGroupOnSlot(buffEntity, character, 3, spellPrefabGUID);
    }
    public static string FormatClassName(PlayerClass classType, bool withSpaces = true)
    {
        string className = withSpaces ? _classNameRegex.Replace(classType.ToString(), " $1") : classType.ToString();

        if (_classColorMap.TryGetValue(classType, out string classColor))
        {
            return $"<color={classColor}>{className}</color>";
        }
        else
        {
            return className;
        }
    }
    public static PlayerClass? ParseClassFromInput(ChatCommandContext ctx, string input)
    {
        if (int.TryParse(input, out int value))
        {
            --value;

            if (!Enum.IsDefined(typeof(PlayerClass), value))
            {
                LocalizationService.HandleReply(ctx,
                    "Invalid class, use '<color=white>.class l</color>' to see options.");
                return null;
            }

            /*
            if (value < 1 || value > 6)
            {
                LocalizationService.HandleReply(ctx, "Invalid class, use <color=white>'.class l'</color> to see options.");
                return null;
            }
            */

            PlayerClass playerClass = (PlayerClass)value;
            return playerClass;
        }
        else
        {
            if (!TryParseClassName(input, out PlayerClass playerClass))
            {
                LocalizationService.HandleReply(ctx, "Invalid class, use <color=white>'.class l'</color> to see options.");
                return null;
            }

            return playerClass;
        }
    }
    static string GetClassSpellName(PrefabGUID prefabGuid)
    {
        string prefabName = prefabGuid.GetLocalizedName();
        if (string.IsNullOrEmpty(prefabName) || prefabName.Equals(NO_NAME) || prefabName.Equals(PRIMARY_ATTACK)) prefabName = prefabGuid.GetPrefabName();

        int prefabIndex = prefabName.IndexOf("PrefabGuid");
        if (prefabIndex > 0) prefabName = prefabName[..prefabIndex].TrimEnd();

        return prefabName;
    }
    public static void GetAbilityJewels()
    {
        _jewelQueryDesc = EntityManager.CreateQueryDesc(_jewelComponents, typeIndices: _typeIndices, options: EntityQueryOptions.IncludeAll);
        GetAbilityJewelsRoutine().Start();
    }
    static IEnumerator GetAbilityJewelsRoutine()
    {
        yield return QueryResultStreamAsync(
            _jewelQueryDesc,
            stream =>
            {
                try
                {
                    using (stream)
                    {
                        foreach (QueryResult result in stream.GetResults())
                        {
                            Entity entity = result.Entity;
                            PrefabGUID prefabGuid = result.ResolveComponentData<PrefabGUID>();
                            JewelInstance jewelInstance = result.ResolveComponentData<JewelInstance>();

                            // Core.Log.LogWarning($"[GetAbilityJewels] {prefabGuid.GetPrefabName()}");

                            if (!jewelInstance.OverrideAbilityType.HasValue()) continue;

                            if (!_abilityJewelMap.TryGetValue(jewelInstance.OverrideAbilityType, out var list))
                            {
                                list = [];
                                _abilityJewelMap[jewelInstance.OverrideAbilityType] = list;
                            }

                            string prefabName = prefabGuid.GetPrefabName().Split(" ", 2)[0];

                            if (prefabName.EndsWith("T01") || prefabName.EndsWith("T02") || prefabName.EndsWith("T03") || prefabName.EndsWith("T04")) continue;
                            else list.Add(entity);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Core.Log.LogWarning($"GenerateAbilityJewelMap() - {ex}");
                }

                // Core.Log.LogWarning($"[GenerateAbilityJewelMap] Jewel map generated - {_abilityJewelMap.Count}");
            }
        );

        // SpellSchoolAbility.TryGetSchoolAbility
        // SpellSchoolMappingSystem
        // Core.SystemService.JewelSpawnSystem.GetRandomJewelAbilityFromSchool
        // JewelSpawnSystem, JewelRegisterSystem, SpellModSyncSystem_Server, SpellModTierCollectionSystem, SpellModCollectionSystem
    }
    public static bool TryParseClassName(string className, out PlayerClass parsedClassType)
    {
        if (Enum.TryParse(className, true, out parsedClassType))
        {
            return true;
        }

        parsedClassType = Enum.GetValues(typeof(PlayerClass))
            .Cast<PlayerClass>()
            .FirstOrDefault(ct => ct.ToString().Contains(className, StringComparison.CurrentCultureIgnoreCase));

        if (!parsedClassType.Equals(default(PlayerClass)))
        {
            return true;
        }

        parsedClassType = default;
        return false;
    }
    public static void ApplyClassBuffs(Entity player, ulong steamId)
    {
        if (steamId.HasClass(out PlayerClass? playerClass) || !playerClass.HasValue)
        {
            // Core.Log.LogInfo($"No player class found - {steamId}");
            return;
        }

        List<PrefabGUID> classBuffs = GetClassBuffs(steamId);
        if (!classBuffs.Any())
        {
            // Core.Log.LogInfo($"No class buffs found - {steamId}!");
            return;
        }

        int levelStep = ConfigService.MaxLevel / classBuffs.Count;
        int playerLevel;

        if (ConfigService.LevelingSystem)
        {
            playerLevel = GetLevel(steamId);
        }
        else
        {
            Equipment equipment = player.Read<Equipment>();
            playerLevel = (int)equipment.GetFullLevel();
        }

        if (ConfigService.PrestigeSystem && steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData[PrestigeType.Experience] > 0)
        {
            playerLevel = ConfigService.MaxLevel;
        }

        if (levelStep <= 0)
        {
            // Core.Log.LogInfo($"Level step invalid - {steamId}");
            return;
        }

        int numBuffsToApply = playerLevel / levelStep;

        if (numBuffsToApply > 0 && numBuffsToApply <= classBuffs.Count)
        {
            numBuffsToApply = Math.Min(numBuffsToApply, classBuffs.Count); // Limit to available buffs

            for (int i = 0; i < numBuffsToApply; i++)
            {
                // Core.Log.LogInfo($"Applying buff {classBuffs[i].GetPrefabName()} - ");
                Buffs.TryApplyPermanentBuff(player, classBuffs[i]);
            }
        }
    }
    public static IEnumerator GlobalSyncClassBuffs(ChatCommandContext ctx) // this really only has a usecase if buffs weren't being properly removed (which they were not, for a version or two >_>), and will be removed when no longer likely to be needed
    {
        List<PlayerInfo> playerCache = [..SteamIdPlayerInfoCache.Values];

        foreach (PlayerInfo playerInfo in playerCache)
        {
            ulong steamId = playerInfo.User.PlatformId;
            SetPlayerBool(steamId, CLASS_BUFFS_KEY, false);
            RemoveAllClassBuffs(playerInfo.CharEntity);
        }

        yield return _longDelay;

        foreach (PlayerInfo playerInfo in playerCache)
        {
            ulong steamId = playerInfo.User.PlatformId;
            SetPlayerBool(steamId, CLASS_BUFFS_KEY, true);
            ApplyClassBuffs(playerInfo.CharEntity, steamId);
        }

        ctx.Reply("Removed all class buffs then applied current class buffs for all players.");
    }
    public static void GlobalPurgeClassBuffs(ChatCommandContext ctx)
    {
        List<PlayerInfo> playerCache = [..SteamIdPlayerInfoCache.Values];

        foreach (PlayerInfo playerInfo in playerCache)
        {
            ulong steamId = playerInfo.User.PlatformId;
            SetPlayerBool(steamId, CLASS_BUFFS_KEY, false);

            RemoveAllClassBuffs(playerInfo.CharEntity);
        }

        ctx.Reply("Removed all class buffs for all players.");
    }
    public static string FormatModifyUnitStatBuffer(Entity buffEntity)
    {
        if (buffEntity.TryGetBuffer<ModifyUnitStatBuff_DOTS>(out var buffer) && !buffer.IsEmpty)
        {
            List<string> formattedStats = [];

            foreach (ModifyUnitStatBuff_DOTS modifyUnitStatBuff in buffer)
            {
                string statName = modifyUnitStatBuff.StatType.ToString();
                float value = modifyUnitStatBuff.Value;
                string formattedValue;

                if (Enum.TryParse<WeaponStatType>(statName, out var unitStat) && WeaponStatFormats.ContainsKey(unitStat))
                {
                    formattedValue = FormatWeaponStatValue(unitStat, value);
                }
                else
                {
                    formattedValue = FormatPercentStatValue(value);
                }

                string colorizedStat = $"<color=#00FFFF>{statName}</color>: <color=white>{formattedValue}</color>";
                formattedStats.Add(colorizedStat);
            }

            return string.Join(", ", formattedStats);
        }

        else return string.Empty;
    }
    static string FormatPercentStatValue(float value)
    {
        return (value * 100).ToString("F0") + "%";
    }
    static string FormatWeaponStatValue(WeaponStatType statType, float value)
    {
        return WeaponStatFormats.TryGetValue(statType, out string format) ? format switch
        {
            "integer" => ((int)value).ToString(),
            "decimal" => value.ToString("F2"),
            "percentage" => (value * 100).ToString("F1") + "%",
            _ => value.ToString()
        } : FormatPercentStatValue(value); // Fallback if the format isn't found
    }
}
