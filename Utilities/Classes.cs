using Bloodcraft.Patches;
using Bloodcraft.Services;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Gameplay.Scripting;
using ProjectM.Gameplay.Systems;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using System.Collections;
using System.Text.RegularExpressions;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using VampireCommandFramework;
using static Bloodcraft.Systems.Expertise.WeaponManager.WeaponStats;
using static Bloodcraft.Systems.Legacies.BloodManager.BloodStats;

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

    static readonly bool _leveling = ConfigService.LevelingSystem;

    static readonly int _maxLevel = ConfigService.MaxLevel;

    static readonly WaitForSeconds _secondDelay = new(1f);
    static readonly Regex _classNameRegex = new("(?<!^)([A-Z])");

    static readonly PrefabGUID _vBloodAbilityBuff = new(1171608023);

    static readonly PrefabGUID _mutantFromBiteBloodBuff = new(-491525099);
    static readonly PrefabGUID _fallenAngel = new(-76116724);

    static readonly PrefabGUID _playerFaction = new(1106458752);

    const float ANGEL_LIFETIME = 12f;
    const string NO_NAME = "No Name";

    static NativeParallelHashMap<PrefabGUID, ItemData> _itemLookup = SystemService.GameDataSystem.ItemHashLookupMap;
    static PrefabLookupMap _prefabLookupMap = PrefabCollectionSystem._PrefabLookupMap;

    static readonly ComponentType[] _jewelComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<JewelInstance>()),
        ComponentType.ReadOnly(Il2CppType.Of<JewelLevelSource>())
    ];

    static readonly Dictionary<PrefabGUID, List<Entity>> _abilityJewelMap = [];

    public enum PlayerClass
    {
        BloodKnight,
        DemonHunter,
        VampireLord,
        ShadowBlade,
        ArcaneSorcerer,
        DeathMage
    }

    public static readonly Dictionary<PlayerClass, (string, string)> ClassWeaponBloodMap = new()
    {
        { PlayerClass.BloodKnight, (ConfigService.BloodKnightWeapon, ConfigService.BloodKnightBlood) },
        { PlayerClass.DemonHunter, (ConfigService.DemonHunterWeapon, ConfigService.DemonHunterBlood) },
        { PlayerClass.VampireLord, (ConfigService.VampireLordWeapon, ConfigService.VampireLordBlood) },
        { PlayerClass.ShadowBlade, (ConfigService.ShadowBladeWeapon, ConfigService.ShadowBladeBlood) },
        { PlayerClass.ArcaneSorcerer, (ConfigService.ArcaneSorcererWeapon, ConfigService.ArcaneSorcererBlood) },
        { PlayerClass.DeathMage, (ConfigService.DeathMageWeapon, ConfigService.DeathMageBlood) }
    };

    public static readonly Dictionary<PlayerClass, (List<int>, List<int>)> ClassWeaponBloodEnumMap = new()
    {
        { PlayerClass.BloodKnight, (Configuration.ParseConfigIntegerString(ConfigService.BloodKnightWeapon), Configuration.ParseConfigIntegerString(ConfigService.BloodKnightBlood)) },
        { PlayerClass.DemonHunter, (Configuration.ParseConfigIntegerString(ConfigService.DemonHunterWeapon), Configuration.ParseConfigIntegerString(ConfigService.DemonHunterBlood)) },
        { PlayerClass.VampireLord, (Configuration.ParseConfigIntegerString(ConfigService.VampireLordWeapon), Configuration.ParseConfigIntegerString(ConfigService.VampireLordBlood)) },
        { PlayerClass.ShadowBlade, (Configuration.ParseConfigIntegerString(ConfigService.ShadowBladeWeapon), Configuration.ParseConfigIntegerString(ConfigService.ShadowBladeBlood)) },
        { PlayerClass.ArcaneSorcerer, (Configuration.ParseConfigIntegerString(ConfigService.ArcaneSorcererWeapon), Configuration.ParseConfigIntegerString(ConfigService.ArcaneSorcererBlood)) },
        { PlayerClass.DeathMage, (Configuration.ParseConfigIntegerString(ConfigService.DeathMageWeapon), Configuration.ParseConfigIntegerString(ConfigService.DeathMageBlood)) }
    };

    public static readonly Dictionary<PlayerClass, string> ClassBuffMap = new()
    {
        { PlayerClass.BloodKnight, ConfigService.BloodKnightBuffs },
        { PlayerClass.DemonHunter, ConfigService.DemonHunterBuffs },
        { PlayerClass.VampireLord, ConfigService.VampireLordBuffs },
        { PlayerClass.ShadowBlade, ConfigService.ShadowBladeBuffs },
        { PlayerClass.ArcaneSorcerer, ConfigService.ArcaneSorcererBuffs },
        { PlayerClass.DeathMage, ConfigService.DeathMageBuffs }
    };

    public static readonly Dictionary<PlayerClass, string> ClassSpellsMap = new()
    {
        { PlayerClass.BloodKnight, ConfigService.BloodKnightSpells },
        { PlayerClass.DemonHunter, ConfigService.DemonHunterSpells },
        { PlayerClass.VampireLord, ConfigService.VampireLordSpells },
        { PlayerClass.ShadowBlade, ConfigService.ShadowBladeSpells },
        { PlayerClass.ArcaneSorcerer, ConfigService.ArcaneSorcererSpells },
        { PlayerClass.DeathMage, ConfigService.DeathMageSpells }
    };
    public static List<int> GetClassBuffs(ulong steamId)
    {
        if (steamId.TryGetPlayerClasses(out var classes) && classes.Keys.Count > 0)
        {
            var playerClass = classes.Keys.FirstOrDefault();
            return Configuration.ParseConfigIntegerString(ClassBuffMap[playerClass]);
        }

        return [];
    }
    public static PlayerClass GetPlayerClass(ulong steamId)
    {
        if (steamId.TryGetPlayerClasses(out var classes))
        {
            return classes.First().Key;
        }
        throw new Exception("Player does not have a class.");
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
    public static bool HasClass(ulong steamId)
    {
        return steamId.TryGetPlayerClasses(out var classes) && classes.Keys.Count > 0;
    }
    public static void RemoveClassBuffs(ChatCommandContext ctx, ulong steamId)
    {
        List<int> buffs = GetClassBuffs(steamId);

        if (buffs.Count == 0) return;

        for (int i = 0; i < buffs.Count; i++)
        {
            if (buffs[i] == 0) continue;

            PrefabGUID buffPrefab = new(buffs[i]);

            if (ServerGameManager.TryGetBuff(ctx.Event.SenderCharacterEntity, buffPrefab.ToIdentifier(), out Entity buffEntity))
            {
                DestroyUtility.Destroy(EntityManager, buffEntity, DestroyDebugReason.TryRemoveBuff);
            }
        }
    }
    public static void ReplyClassBuffs(ChatCommandContext ctx, PlayerClass playerClass)
    {
        List<int> perks = Configuration.ParseConfigIntegerString(ClassBuffMap[playerClass]);

        if (perks.Count == 0)
        {
            LocalizationService.HandleReply(ctx, $"{playerClass} buffs not found.");
            return;
        }

        int step = ConfigService.MaxLevel / perks.Count;

        var classBuffs = perks.Select((perk, index) =>
        {
            int level = (index + 1) * step;
            string prefab = new PrefabGUID(perk).GetPrefabName();
            int prefabIndex = prefab.IndexOf("Prefab");
            if (prefabIndex != -1)
            {
                prefab = prefab[..prefabIndex].TrimEnd();
            }
            return $"<color=yellow>{index + 1}</color>| <color=white>{prefab}</color> at level <color=green>{level}</color>";
        }).ToList();

        for (int i = 0; i < classBuffs.Count; i += 4) // Using batches of 4 for better readability
        {
            var batch = classBuffs.Skip(i).Take(4);
            string replyMessage = string.Join(", ", batch);
            LocalizationService.HandleReply(ctx, $"{playerClass} buffs: {replyMessage}");
        }
    }
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

            for (int i = 0; i < allStats.Count; i += 6)
            {
                var batch = allStats.Skip(i).Take(6);
                string replyMessage = string.Join(", ", batch);
                LocalizationService.HandleReply(ctx, $"{FormatClassName(playerClass)} stat synergies[x<color=white>{ConfigService.StatSynergyMultiplier}</color>]: {replyMessage}");
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, $"Couldn't find stat synergies for {FormatClassName(playerClass)}...");
        }
    }
    public static void ReplyClassSpells(ChatCommandContext ctx, PlayerClass playerClass)
    {
        List<int> spells = Configuration.ParseConfigIntegerString(ClassSpellsMap[playerClass]);

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

        for (int i = 0; i < classSpells.Count; i += 4)
        {
            var batch = classSpells.Skip(i).Take(4);
            string replyMessage = string.Join(", ", batch);
            LocalizationService.HandleReply(ctx, $"{FormatClassName(playerClass)} spells: {replyMessage}");
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
                              .FirstOrDefault(pc => pc.ToString().Contains(classType, StringComparison.OrdinalIgnoreCase));

        if (!parsedClassType.Equals(default(PlayerClass)))
        {
            return true;
        }

        parsedClassType = default;
        return false;
    }
    public static void UpdateClassData(Entity character, PlayerClass parsedClassType, Dictionary<PlayerClass, (List<int>, List<int>)> classes, ulong steamId)
    {
        classes.Clear();

        var weaponConfigEntry = ClassWeaponBloodMap[parsedClassType].Item1;
        var bloodConfigEntry = ClassWeaponBloodMap[parsedClassType].Item2;

        var classWeaponStats = Configuration.ParseConfigIntegerString(weaponConfigEntry);
        var classBloodStats = Configuration.ParseConfigIntegerString(bloodConfigEntry);

        classes[parsedClassType] = (classWeaponStats, classBloodStats);
        steamId.SetPlayerClasses(classes);

        Buffs.HandleClassBuffs(character, steamId);
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

                    if (JewelEquipUtilitiesServer.TryUnequipJewel(EntityManager, ref _itemLookup, inventoryEntity, abilityGroup))
                    {
                        tryEquip = true;
                    }
                }

                if (firstBuffer.IsIndexWithinRange(index) && buffEntity.Exists())
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

                    if (JewelEquipUtilitiesServer.TryUnequipJewel(EntityManager, ref _itemLookup, inventoryEntity, abilityGroup))
                    {
                        tryEquip = true;
                    }
                }

                if (firstBuffer.IsIndexWithinRange(index) && buffEntity.Exists())
                {
                    firstBuffer.RemoveAt(index);
                    DestroyUtility.Destroy(EntityManager, buffEntity, DestroyDebugReason.TryRemoveBuff);
                }
            }

            VBloodAbilityUtilities.InstantiateBuff(EntityManager, ActivateVBloodAbilitySystem._BuffSpawnerSystemData, character, PrefabCollectionSystem._PrefabGuidToEntityMap[_vBloodAbilityBuff], spellPrefabGUID, 3);

            if (ServerGameManager.TryGetBuffer<VBloodAbilityBuffEntry>(character, out var secondBuffer))
            {
                foreach (VBloodAbilityBuffEntry abilityEntry in secondBuffer)
                {
                    if (abilityEntry.ActiveAbility.Equals(spellPrefabGUID))
                    {
                        abilityEntry.ActiveBuff.With((ref VBloodAbilityReplaceBuff vBloodAbilityReplaceBuff) =>
                        {
                            vBloodAbilityReplaceBuff.AbilityType = vBloodAbilityData.AbilityType;
                        });

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

                    if (JewelEquipUtilitiesServer.TryUnequipJewel(EntityManager, ref _itemLookup, inventoryEntity, abilityGroup))
                    {
                        tryEquip = true;
                    }
                }

                if (firstBuffer.IsIndexWithinRange(index) && buffEntity.Exists())
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

        if (replaceBuffer.IsIndexWithinRange(toRemove)) replaceBuffer.RemoveAt(toRemove);

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

        if (replaceBuffer.IsIndexWithinRange(toRemove)) replaceBuffer.RemoveAt(toRemove);

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

    static readonly Dictionary<PlayerClass, string> _classColorMap = new()
    {
        { PlayerClass.ShadowBlade, "#A020F0" },
        { PlayerClass.DemonHunter, "#FFD700" },      
        { PlayerClass.BloodKnight, "#FF0000" },        
        { PlayerClass.ArcaneSorcerer, "#008080" },   
        { PlayerClass.VampireLord, "#00FFFF" },      
        { PlayerClass.DeathMage, "#00FF00" }   
    };

    public static readonly Dictionary<PrefabGUID, string> ShinyBuffColorHexMap = new()
    {
        { new(348724578), "#A020F0" },   // ignite purple (Hex: A020F0)
        { new(-1576512627), "#FFD700" },  // static yellow (Hex: FFD700)
        { new(-1246704569), "#FF0000" },  // leech red (Hex: FF0000)
        { new(1723455773), "#008080" },   // weaken teal (Hex: 008080)
        { new(27300215), "#00FFFF" },     // chill cyan (Hex: 00FFFF)
        { new(-325758519), "#00FF00" }    // condemn green (Hex: 00FF00)
    };
    public static string FormatClassName(PlayerClass classType)
    {
        string className = _classNameRegex.Replace(classType.ToString(), " $1");

        if (_classColorMap.TryGetValue(classType, out string classColor))
        {
            return $"<color={classColor}>{className}</color>";
        }
        else
        {
            return className;
        }
    }
    public static void HandleDeathMageMutantBuffScriptSpawn(Entity entity, Entity player, ulong steamId)
    {
        PlayerClass playerClass = GetPlayerClass(steamId);

        if (playerClass.Equals(PlayerClass.DeathMage) && entity.GetBuffTarget().TryGetPlayer(out player))
        {
            List<PrefabGUID> perks = Configuration.ParseConfigIntegerString(ClassBuffMap[playerClass]).Select(x => new PrefabGUID(x)).ToList();
            int indexOfBuff = perks.IndexOf(_mutantFromBiteBloodBuff);

            if (indexOfBuff != -1)
            {
                int step = _maxLevel / perks.Count;
                int level = (_leveling && steamId.TryGetPlayerExperience(out var playerExperience)) ? playerExperience.Key : (int)player.Read<Equipment>().GetFullLevel();

                if (level >= step * (indexOfBuff + 1))
                {
                    var buffer = entity.ReadBuffer<RandomMutant>();

                    RandomMutant randomMutant = buffer[0];
                    randomMutant.Mutant = _fallenAngel;
                    buffer[0] = randomMutant;

                    buffer.RemoveAt(1);

                    entity.With((ref BloodBuff_BiteToMutant_DataShared bloodBuff_BiteToMutant_DataShared) =>
                    {
                        bloodBuff_BiteToMutant_DataShared.MaxBonus = 1f;
                        bloodBuff_BiteToMutant_DataShared.MinBonus = 1f;
                    });
                }
            }
        }
    }
    public static void HandleDeathMageBiteTriggerBuffSpawnServer(Entity player, ulong steamId)
    {
        if (player.TryGetBuff(_mutantFromBiteBloodBuff, out Entity buffEntity) && buffEntity.TryGetBuffer<RandomMutant>(out var buffer))
        {
            if (buffer.Length == 1 && buffer[0].Mutant.Equals(_fallenAngel))
            {
                if (!BuffSystemSpawnPatches.DeathMageMutantTriggerCounts.ContainsKey(steamId))
                {
                    BuffSystemSpawnPatches.DeathMageMutantTriggerCounts.TryAdd(steamId, 0);
                }

                if (BuffSystemSpawnPatches.DeathMageMutantTriggerCounts[steamId] < 2)
                {
                    BuffSystemSpawnPatches.DeathMageMutantTriggerCounts[steamId] += 1;

                    if (BuffSystemSpawnPatches.DeathMageMutantTriggerCounts[steamId] == 1)
                    {
                        BuffSystemSpawnPatches.DeathMagePlayerAngelSpawnOrder.Enqueue(player);

                        Core.StartCoroutine(PassiveBuffModificationWithDelayRoutine(buffEntity, 0f));
                    }
                }
                else if (BuffSystemSpawnPatches.DeathMageMutantTriggerCounts[steamId] >= 2)
                {
                    BuffSystemSpawnPatches.DeathMageMutantTriggerCounts[steamId] = 0;

                    Core.StartCoroutine(PassiveBuffModificationWithDelayRoutine(buffEntity, 1f));
                }
            }
        }
    }
    public static void ModifyFallenAngelForDeathMage(Entity fallenAngel, Entity player)
    {
        fallenAngel.SetTeam(player);
        fallenAngel.SetFaction(_playerFaction);
        fallenAngel.NothingLivesForever(ANGEL_LIFETIME);
    }
    static string GetClassSpellName(PrefabGUID prefabGuid)
    {
        string prefabName = prefabGuid.GetLocalizedName();
        if (string.IsNullOrEmpty(prefabName) || prefabName.Equals(NO_NAME)) prefabName = prefabGuid.GetPrefabName();

        int prefabIndex = prefabName.IndexOf("PrefabGuid");
        if (prefabIndex > 0) prefabName = prefabName[..prefabIndex].TrimEnd();

        return prefabName;
    }
    static IEnumerator PassiveBuffModificationWithDelayRoutine(Entity buffEntity, float chance)
    {
        yield return _secondDelay;

        buffEntity.With((ref BloodBuff_BiteToMutant_DataShared bloodBuff_BiteToMutant_DataShared) =>
        {
            bloodBuff_BiteToMutant_DataShared.MaxBonus = chance;
            bloodBuff_BiteToMutant_DataShared.MinBonus = chance;
        });
    }
    public static void GenerateAbilityJewelMap()
    {
        EntityQuery jewelQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = _jewelComponents,
            Options = EntityQueryOptions.IncludeAll
        });

        try
        {
            IEnumerable<Entity> jewelEntities = Queries.GetEntitiesEnumerable(jewelQuery);

            foreach (Entity entity in jewelEntities)
            {
                if (!entity.TryGetComponent(out PrefabGUID prefab)) continue;
                else if (entity.TryGetComponent(out JewelInstance jewelInstance) && jewelInstance.OverrideAbilityType.HasValue())
                {
                    if (!_abilityJewelMap.ContainsKey(jewelInstance.OverrideAbilityType))
                    {
                        _abilityJewelMap.Add(jewelInstance.OverrideAbilityType, []);
                    }

                    string prefabName = prefab.GetPrefabName().Split(" ", 2)[0];

                    if (prefabName.EndsWith("T01") || prefabName.EndsWith("T02") || prefabName.EndsWith("T03") || prefabName.EndsWith("T04")) continue;
                    else _abilityJewelMap[jewelInstance.OverrideAbilityType].Add(entity);
                }
            }
        }
        finally
        {
            jewelQuery.Dispose();
        }
    }
    public static bool TryParseClassName(string className, out PlayerClass parsedClassType)
    {
        if (Enum.TryParse(className, true, out parsedClassType))
        {
            return true;
        }

        parsedClassType = Enum.GetValues(typeof(PlayerClass))
            .Cast<PlayerClass>()
            .FirstOrDefault(ct => ct.ToString().Contains(className, StringComparison.OrdinalIgnoreCase));

        if (!parsedClassType.Equals(default(PlayerClass)))
        {
            return true;
        }

        parsedClassType = default;
        return false;
    }
}
