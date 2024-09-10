using Bloodcraft.Services;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;
using static Bloodcraft.Systems.Experience.LevelingSystem;

namespace Bloodcraft.Utilities;

internal static class ClassUtilities
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static EntityCommandBufferSystem EntityCommandBufferSystem => SystemService.EntityCommandBufferSystem;
    public static List<int> GetClassBuffs(ulong steamId)
    {
        if (steamId.TryGetPlayerClasses(out var classes) && classes.Keys.Count > 0)
        {
            var playerClass = classes.Keys.FirstOrDefault();
            return ConfigUtilities.ParseConfigString(ClassPrestigeBuffsMap[playerClass]);
        }
        return [];
    }
    public static PlayerClasses GetPlayerClass(ulong steamId)
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


        RemoveClassBuffs(ctx, steamId);
        return true;
    }
    public static bool HasClass(ulong steamId)
    {
        return steamId.TryGetPlayerClasses(out var classes) && classes.Keys.Count > 0;
    }
    public static void RemoveClassBuffs(ChatCommandContext ctx, ulong steamId)
    {
        List<int> buffs = GetClassBuffs(steamId);
        var buffSpawner = BuffUtility.BuffSpawner.Create(ServerGameManager);
        var entityCommandBuffer = EntityCommandBufferSystem.CreateCommandBuffer();

        if (buffs.Count == 0) return;

        for (int i = 0; i < buffs.Count; i++)
        {
            if (buffs[i] == 0) continue;
            PrefabGUID buffPrefab = new(buffs[i]);
            if (ServerGameManager.HasBuff(ctx.Event.SenderCharacterEntity, buffPrefab.ToIdentifier()))
            {
                BuffUtility.TryRemoveBuff(ref buffSpawner, entityCommandBuffer, buffPrefab.ToIdentifier(), ctx.Event.SenderCharacterEntity);
            }
        }
    }
    public static void ReplyClassBuffs(ChatCommandContext ctx, PlayerClasses playerClass)
    {
        List<int> perks = ConfigUtilities.ParseConfigString(ClassPrestigeBuffsMap[playerClass]);

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
    public static void ReplyClassSpells(ChatCommandContext ctx, PlayerClasses playerClass)
    {
        List<int> perks = ConfigUtilities.ParseConfigString(ClassSpellsMap[playerClass]);

        if (perks.Count == 0)
        {
            LocalizationService.HandleReply(ctx, $"{playerClass} spells not found.");
            return;
        }

        var classSpells = perks.Select(perk =>
        {
            string prefab = new PrefabGUID(perk).LookupName();
            int prefabIndex = prefab.IndexOf("Prefab");
            if (prefabIndex != -1)
            {
                prefab = prefab[..prefabIndex].TrimEnd();
            }
            return $"<color=white>{prefab}</color>";
        }).ToList();

        for (int i = 0; i < classSpells.Count; i += 6)
        {
            var batch = classSpells.Skip(i).Take(6);
            string replyMessage = string.Join(", ", batch);
            LocalizationService.HandleReply(ctx, $"{playerClass} spells: {replyMessage}");
        }
    }
    public static bool TryParseClass(string classType, out PlayerClasses parsedClassType)
    {
        // Attempt to parse the classType string to the PlayerClasses enum.
        if (Enum.TryParse(classType, true, out parsedClassType))
        {
            return true; // Successfully parsed
        }

        // If the initial parse failed, try to find a matching PlayerClasses enum value containing the input string.
        parsedClassType = Enum.GetValues(typeof(PlayerClasses))
                              .Cast<PlayerClasses>()
                              .FirstOrDefault(pc => pc.ToString().Contains(classType, StringComparison.OrdinalIgnoreCase));

        // Check if a valid enum value was found that contains the input string.
        if (!parsedClassType.Equals(default(PlayerClasses)))
        {
            return true; // Found a matching enum value
        }

        // If no match is found, return false and set the out parameter to default value.
        parsedClassType = default;
        return false; // Parsing failed
    }
    public static void UpdateClassData(Entity character, PlayerClasses parsedClassType, Dictionary<PlayerClasses, (List<int>, List<int>)> classes, ulong steamId)
    {
        var weaponConfigEntry = ClassWeaponBloodMap[parsedClassType].Item1;
        var bloodConfigEntry = ClassWeaponBloodMap[parsedClassType].Item2;
        var classWeaponStats = ConfigUtilities.ParseConfigString(weaponConfigEntry);
        var classBloodStats = ConfigUtilities.ParseConfigString(bloodConfigEntry);

        classes[parsedClassType] = (classWeaponStats, classBloodStats);
        steamId.SetPlayerClasses(classes);

        FromCharacter fromCharacter = new()
        {
            Character = character,
            User = character.Read<PlayerCharacter>().UserEntity,
        };
        BuffUtilities.
                ApplyClassBuffs(character, steamId, fromCharacter);
    }
}
