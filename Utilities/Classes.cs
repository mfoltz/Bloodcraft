using Bloodcraft.Services;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using VampireCommandFramework;
using static Bloodcraft.Systems.Leveling.LevelingSystem;

namespace Bloodcraft.Utilities;
internal static class Classes
{
    static EntityManager EntityManager => Core.EntityManager;

    static EntityManager EntityManagerRef = Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static PrefabCollectionSystem PrefabCollectionSystem => SystemService.PrefabCollectionSystem;
    static ActivateVBloodAbilitySystem ActivateVBloodAbilitySystem => SystemService.ActivateVBloodAbilitySystem;
    static ReplaceAbilityOnSlotSystem ReplaceAbilityOnSlotSystem => SystemService.ReplaceAbilityOnSlotSystem;

    static readonly PrefabGUID VBloodAbilityBuff = new(1171608023);

    static NativeParallelHashMap<PrefabGUID, ItemData> itemLookup = SystemService.GameDataSystem.ItemHashLookupMap;
    static PrefabLookupMap PrefabLookupMap = PrefabCollectionSystem._PrefabLookupMap;

    static readonly ComponentType[] JewelComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<JewelInstance>()),
        ComponentType.ReadOnly(Il2CppType.Of<JewelLevelSource>())
    ];

    static EntityQuery JewelQuery;

    static readonly Dictionary<PrefabGUID, List<Entity>> AbilityJewelMap = [];
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
    public static bool HandleClassChangeItem(ChatCommandContext ctx, ulong steamId)
    {
        PrefabGUID item = new(ConfigService.ChangeClassItem);
        int quantity = ConfigService.ChangeClassQuantity;

        if (!InventoryUtilities.TryGetInventoryEntity(EntityManager, ctx.User.LocalCharacter._Entity, out var inventoryEntity) ||
            ServerGameManager.GetInventoryItemCount(inventoryEntity, item) < quantity)
        {
            LocalizationService.HandleReply(ctx, $"You do not have the required item to change classes ({item.GetPrefabName()}x{quantity})");
            return false;
        }

        if (!ServerGameManager.TryRemoveInventoryItem(inventoryEntity, item, quantity))
        {
            LocalizationService.HandleReply(ctx, $"Failed to remove the required item ({item.GetPrefabName()}x{quantity})");
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
            string prefab = new PrefabGUID(perk).LookupName();
            int prefabIndex = prefab.IndexOf("Prefab");
            if (prefabIndex != -1)
            {
                prefab = prefab[..prefabIndex].TrimEnd();
            }
            return $"<color=white>{prefab}</color> at level <color=yellow>{level}</color>";
        }).ToList();

        for (int i = 0; i < classBuffs.Count; i += 6)
        {
            var batch = classBuffs.Skip(i).Take(6);
            string replyMessage = string.Join(", ", batch);
            LocalizationService.HandleReply(ctx, $"{playerClass} buffs: {replyMessage}");
        }
    }
    public static void ReplyClassSpells(ChatCommandContext ctx, PlayerClass playerClass)
    {
        List<int> perks = Configuration.ParseConfigIntegerString(ClassSpellsMap[playerClass]);

        if (perks.Count == 0)
        {
            LocalizationService.HandleReply(ctx, $"{playerClass} spells not found.");
            return;
        }

        var classSpells = perks.Select(perk =>
        {
            string prefab = new PrefabGUID(perk).GetPrefabName();
            int prefabIndex = prefab.IndexOf("Prefab");
            if (prefabIndex != -1)
            {
                prefab = prefab[..prefabIndex].TrimEnd();
            }
            if (prefab.Contains("Name")) prefab = new PrefabGUID(perk).LookupName();
            return $"<color=white>{prefab}</color>";
        }).ToList();

        for (int i = 0; i < classSpells.Count; i += 6)
        {
            var batch = classSpells.Skip(i).Take(6);
            string replyMessage = string.Join(", ", batch);
            LocalizationService.HandleReply(ctx, $"{playerClass} spells: {replyMessage}");
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

        Buffs.ApplyClassBuffs(character, steamId);
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

                if (InventoryUtilities.TryGetInventoryEntity(EntityManager, character, out Entity inventoryEntity) && AbilityJewelMap.TryGetValue(oldAbility, out List<Entity> jewelEntities))
                {
                    foreach (Entity jewel in jewelEntities)
                    {
                        if (JewelEquipUtilitiesServer.TryGetEquippedJewel(EntityManager, character, jewel, out equippedJewelEntity) && equippedJewelEntity.Exists())
                        {
                            break;
                        }
                    }

                    if (JewelEquipUtilitiesServer.TryUnequipJewel(EntityManager, ref itemLookup, inventoryEntity, abilityGroup))
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
                JewelEquipUtilitiesServer.TryEquipJewel(ref EntityManagerRef, ref PrefabLookupMap, character, slot);
            }

            RemoveNPCSpell(character);
        }
        else
        {
            RemoveNPCSpell(character);
        }

        //EclipseService.SendClientAbilityData(character);
    }
    public static void UpdateShift(ChatCommandContext ctx, Entity character, PrefabGUID spellPrefabGUID)
    {
        if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(spellPrefabGUID, out Entity ability) && ability.TryGetComponent(out VBloodAbilityData vBloodAbilityData))
        {
            bool tryEquip = false;

            Entity inventoryEntity = Entity.Null;
            Entity equippedJewelEntity = Entity.Null;
            Entity abilityGroup = ServerGameManager.GetAbilityGroup(character, 3);
            string spellName = spellPrefabGUID.GetPrefabName();

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

                if (InventoryUtilities.TryGetInventoryEntity(EntityManager, character, out inventoryEntity) && AbilityJewelMap.TryGetValue(oldAbility, out List<Entity> jewelEntities))
                {
                    foreach (Entity jewel in jewelEntities)
                    {
                        if (JewelEquipUtilitiesServer.TryGetEquippedJewel(EntityManager, character, jewel, out equippedJewelEntity) && equippedJewelEntity.Exists())
                        {
                            break;
                        }
                    }

                    if (JewelEquipUtilitiesServer.TryUnequipJewel(EntityManager, ref itemLookup, inventoryEntity, abilityGroup))
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

            VBloodAbilityUtilities.InstantiateBuff(EntityManager, ActivateVBloodAbilitySystem._BuffSpawnerSystemData, character, PrefabCollectionSystem._PrefabGuidToEntityMap[VBloodAbilityBuff], spellPrefabGUID, 3);

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
                JewelEquipUtilitiesServer.TryEquipJewel(ref EntityManagerRef, ref PrefabLookupMap, character, slot);
            }

            LocalizationService.HandleReply(ctx, $"Shift spell: <color=#CBC3E3>{spellName}</color>");
        }
        else if (spellPrefabGUID.HasValue())
        {
            Entity inventoryEntity = Entity.Null;
            Entity equippedJewelEntity = Entity.Null;
            Entity abilityGroup = ServerGameManager.GetAbilityGroup(character, 3);
            string spellName = spellPrefabGUID.LookupName();

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

                if (InventoryUtilities.TryGetInventoryEntity(EntityManager, character, out inventoryEntity) && AbilityJewelMap.TryGetValue(oldAbility, out List<Entity> jewelEntities))
                {
                    foreach (Entity jewel in jewelEntities)
                    {
                        if (JewelEquipUtilitiesServer.TryGetEquippedJewel(EntityManager, character, jewel, out equippedJewelEntity) && equippedJewelEntity.Exists())
                        {
                            break;
                        }
                    }

                    if (JewelEquipUtilitiesServer.TryUnequipJewel(EntityManager, ref itemLookup, inventoryEntity, abilityGroup))
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
                    JewelEquipUtilitiesServer.TryEquipJewel(ref EntityManagerRef, ref PrefabLookupMap, character, slot);
                }

                LocalizationService.HandleReply(ctx, $"Shift spell: <color=#CBC3E3>{spellName}</color>");
            }
            else
            {
                HandleNPCSpell(character, spellPrefabGUID);
                LocalizationService.HandleReply(ctx, $"Shift spell: <color=#CBC3E3>{spellName}</color>");
            }
        }

        //EclipseService.SendClientAbilityData(character);
    }
    static void RemoveNPCSpell(Entity character)
    {
        Entity buffEntity = Entity.Null;
        var buffer = character.ReadBuffer<BuffBuffer>();

        for (int i = 0; i < buffer.Length; i++)
        {
            BuffBuffer item = buffer[i];
            if (item.PrefabGuid.LookupName().StartsWith("EquipBuff_Weapon"))
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
            if (item.PrefabGuid.LookupName().StartsWith("EquipBuff_Weapon"))
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
    public static void GenerateAbilityJewelMap()
    {
        JewelQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = JewelComponents,
            Options = EntityQueryOptions.IncludeAll
        });

        try
        {
            IEnumerable<Entity> jewelEntities = Queries.GetEntitiesEnumerable(JewelQuery);
            foreach (Entity entity in jewelEntities)
            {
                if (!entity.TryGetComponent(out PrefabGUID prefab)) continue;
                else if (entity.TryGetComponent(out JewelInstance jewelInstance) && jewelInstance.OverrideAbilityType.HasValue())
                {
                    if (!AbilityJewelMap.ContainsKey(jewelInstance.OverrideAbilityType))
                    {
                        AbilityJewelMap.Add(jewelInstance.OverrideAbilityType, []);
                    }

                    string prefabName = entity.Read<PrefabGUID>().LookupName().Split(" ", 2)[0];

                    if (prefabName.EndsWith("T01") || prefabName.EndsWith("T02") || prefabName.EndsWith("T03") || prefabName.EndsWith("T04")) continue;
                    else AbilityJewelMap[jewelInstance.OverrideAbilityType].Add(entity);
                }
            }
        }
        finally
        {
            JewelQuery.Dispose();
        }
    }
    public static bool TryParseClassName(string className, out PlayerClass parsedClassType)
    {
        // Attempt to parse the className string to the PlayerClasses enum.
        if (Enum.TryParse(className, true, out parsedClassType))
        {
            return true; // Successfully parsed
        }

        // If the initial parse failed, try to find a matching PlayerClasses enum value containing the input string.
        parsedClassType = Enum.GetValues(typeof(PlayerClass))
                             .Cast<PlayerClass>()
                             .FirstOrDefault(ct => ct.ToString().Contains(className, StringComparison.OrdinalIgnoreCase));

        // Check if a valid enum value was found that contains the input string.
        if (!parsedClassType.Equals(default(PlayerClass)))
        {
            return true; // Found a matching enum value
        }

        // If no match is found, return false and set the out parameter to default value.
        parsedClassType = default;
        return false; // Parsing failed
    }
}
