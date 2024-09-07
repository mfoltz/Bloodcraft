using Bloodcraft.Commands;
using Bloodcraft.Patches;
using Bloodcraft.Services;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Systems.Quests;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using VampireCommandFramework;
using static Bloodcraft.Systems.Experience.LevelingSystem;
using static Bloodcraft.Systems.Familiars.FamiliarSummonSystem;
using static Bloodcraft.Patches.LinkMinionToOwnerOnSpawnSystemPatch;

namespace Bloodcraft;
internal static class Utilities
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static DebugEventsSystem DebugEventsSystem => SystemService.DebugEventsSystem;
    static EntityCommandBufferSystem EntityCommandBufferSystem => SystemService.EntityCommandBufferSystem;

    // need to organize methods although it is convenient to access them like this >_>
    public static IEnumerable<Entity> GetEntitiesEnumerable(EntityQuery entityQuery, bool checkBuffBuffer = false) // not sure if need to actually check for empty buff buffer for quest targets but don't really want to find out
    {
        JobHandle handle = GetEntities(entityQuery, out NativeArray<Entity> entities, Allocator.TempJob);
        handle.Complete();
        try
        {
            foreach (Entity entity in entities)
            {
                if (EntityManager.Exists(entity))
                {
                    if (checkBuffBuffer && entity.ReadBuffer<BuffBuffer>().IsEmpty) continue;
                    yield return entity;
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
    static JobHandle GetEntities(EntityQuery entityQuery, out NativeArray<Entity> entities, Allocator allocator = Allocator.TempJob)
    {
        entities = entityQuery.ToEntityArray(allocator);
        return default;
    }
    public static bool GetPlayerBool(ulong steamId, string boolKey)
    {
        return steamId.TryGetPlayerBools(out var bools) && bools[boolKey];
    }
    public static void TogglePlayerBool(ulong steamId, string boolKey)
    {
        if (steamId.TryGetPlayerBools(out var bools))
        {
            bools[boolKey] = !bools[boolKey];
            steamId.SetPlayerBools(bools);
        }
    }
    public static void SetPlayerBool(ulong steamId, string boolKey, bool value)
    {
        if (steamId.TryGetPlayerBools(out var bools))
        {
            bools[boolKey] = value;
            steamId.SetPlayerBools(bools);
        }
    }
    public static bool HasDismissed(ulong steamId, out Entity familiar)
    {
        familiar = Entity.Null;
        if (steamId.TryGetFamiliarActives(out var actives) && actives.Familiar.Exists())
        {
            familiar = actives.Familiar;
            return true;
        }
        return false;
    }
    public static void ReturnFamiliar(Entity player, Entity familiar)
    {
        Follower following = familiar.Read<Follower>();
        following.ModeModifiable._Value = 1;
        familiar.Write(following);

        float3 playerPos = player.Read<Translation>().Value;
        float distance = UnityEngine.Vector3.Distance(familiar.Read<Translation>().Value, playerPos);

        if (distance > 25f)
        {
            familiar.Write(new LastTranslation { Value = playerPos });
            familiar.Write(new Translation { Value = playerPos });
        }
    }
    public static Entity FindPlayerFamiliar(Entity character)
    {
        if (!character.Has<FollowerBuffer>()) return Entity.Null;

        var followers = character.ReadBuffer<FollowerBuffer>();
        ulong steamId = character.GetSteamId();

        if (!followers.IsEmpty) // if buffer not empty check here first, only need the rest if familiar is disabled via call/dismiss since that removes from followerBuffer to enable waygate use and such
        {
            foreach (FollowerBuffer follower in followers)
            {
                Entity familiar = follower.Entity._Entity;
                if (familiar.Has<BlockFeedBuff>() && familiar.Exists()) return familiar;
            }
        }
        else if (HasDismissed(steamId, out Entity familiar)) return familiar;
        return Entity.Null;
    }
    public static void ClearFamiliarActives(ulong steamId)
    {
        if (steamId.TryGetFamiliarActives(out var actives))
        {
            actives = (Entity.Null, 0);
            steamId.SetFamiliarActives(actives);
        }
    }
    public static void HandleFamiliarMinions(Entity familiar) //  need to see if game will handle familiar minions as player minions without extra effort, that would be neat
    {
        if (FamiliarMinions.ContainsKey(familiar))
        {
            foreach (Entity minion in FamiliarMinions[familiar])
            {
                DestroyUtility.CreateDestroyEvent(EntityManager, minion, DestroyReason.Default, DestroyDebugReason.None);
            }
            FamiliarMinions.Remove(familiar);
        }
    }
    public static void HandleVisual(Entity entity, PrefabGUID visual)
    {
        ApplyBuffDebugEvent applyBuffDebugEvent = new()
        {
            BuffPrefabGUID = visual,
        };

        FromCharacter fromCharacter = new()
        {
            Character = entity,
            User = entity
        };

        DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
        if (ServerGameManager.TryGetBuff(entity, applyBuffDebugEvent.BuffPrefabGUID.ToIdentifier(), out Entity buff))
        {
            if (buff.Has<Buff>())
            {
                BuffCategory component = buff.Read<BuffCategory>();
                component.Groups = BuffCategoryFlag.None;
                buff.Write(component);
            }
            if (buff.Has<CreateGameplayEventsOnSpawn>())
            {
                buff.Remove<CreateGameplayEventsOnSpawn>();
            }
            if (buff.Has<GameplayEventListeners>())
            {
                buff.Remove<GameplayEventListeners>();
            }
            if (buff.Has<LifeTime>())
            {
                LifeTime lifetime = buff.Read<LifeTime>();
                lifetime.Duration = -1;
                lifetime.EndAction = LifeTimeEndAction.None;
                buff.Write(lifetime);
            }
            if (buff.Has<RemoveBuffOnGameplayEvent>())
            {
                buff.Remove<RemoveBuffOnGameplayEvent>();
            }
            if (buff.Has<RemoveBuffOnGameplayEventEntry>())
            {
                buff.Remove<RemoveBuffOnGameplayEventEntry>();
            }
            if (buff.Has<DealDamageOnGameplayEvent>())
            {
                buff.Remove<DealDamageOnGameplayEvent>();
            }
            if (buff.Has<HealOnGameplayEvent>())
            {
                buff.Remove<HealOnGameplayEvent>();
            }
            if (buff.Has<BloodBuffScript_ChanceToResetCooldown>())
            {
                buff.Remove<BloodBuffScript_ChanceToResetCooldown>();
            }
            if (buff.Has<ModifyMovementSpeedBuff>())
            {
                buff.Remove<ModifyMovementSpeedBuff>();
            }
            if (buff.Has<ApplyBuffOnGameplayEvent>())
            {
                buff.Remove<ApplyBuffOnGameplayEvent>();
            }
            if (buff.Has<DestroyOnGameplayEvent>())
            {
                buff.Remove<DestroyOnGameplayEvent>();
            }
            if (buff.Has<WeakenBuff>())
            {
                buff.Remove<WeakenBuff>();
            }
            if (buff.Has<ReplaceAbilityOnSlotBuff>())
            {
                buff.Remove<ReplaceAbilityOnSlotBuff>();
            }
            if (buff.Has<AmplifyBuff>())
            {
                buff.Remove<AmplifyBuff>();
            }
        }
    }
    public static bool TryParseFamiliarStat(string statType, out FamiliarStatType parsedStatType)
    {
        if (Enum.TryParse(statType, true, out parsedStatType))
        {
            return true;
        }

        parsedStatType = Enum.GetValues(typeof(FamiliarStatType))
            .Cast<FamiliarStatType>()
            .FirstOrDefault(pt => pt.ToString().Contains(statType, StringComparison.OrdinalIgnoreCase));

        if (!parsedStatType.Equals(default(FamiliarStatType)))
        {
            return true;
        }

        parsedStatType = default;
        return false;
    }
    public static void ToggleShinies(ChatCommandContext ctx, ulong steamId)
    {
        TogglePlayerBool(steamId, "FamiliarVisual");
        LocalizationService.HandleReply(ctx, GetPlayerBool(steamId, "FamiliarVisual") ? "Shiny familiars <color=green>enabled</color>." : "Shiny familiars <color=red>disabled</color>.");
    }
    public static void ToggleVBloodEmotes(ChatCommandContext ctx, ulong steamId)
    {
        TogglePlayerBool(steamId, "VBloodEmotes");
        LocalizationService.HandleReply(ctx, GetPlayerBool(steamId, "VBloodEmotes") ? "VBlood Emotes <color=green>enabled</color>." : "VBlood Emotes <color=red>disabled</color>.");
    }
    public static void QuestRewards()
    {
        List<PrefabGUID> questRewards = ParseConfigString(ConfigService.QuestRewards).Select(x => new PrefabGUID(x)).ToList();
        List<int> rewardAmounts = [.. ParseConfigString(ConfigService.QuestRewardAmounts)];
        QuestSystem.QuestRewards = questRewards.Zip(rewardAmounts, (reward, amount) => new { reward, amount }).ToDictionary(x => x.reward, x => x.amount);
    }
    public static void StarterKit()
    {
        List<PrefabGUID> kitPrefabs = ParseConfigString(ConfigService.KitPrefabs).Select(x => new PrefabGUID(x)).ToList();
        List<int> kitAmounts = [.. ParseConfigString(ConfigService.KitQuantities)];
        MiscCommands.KitPrefabs = kitPrefabs.Zip(kitAmounts, (item, amount) => new { item, amount }).ToDictionary(x => x.item, x => x.amount);
    }
    public static void FamiliarBans()
    {
        List<int> unitBans = ParseConfigString(ConfigService.BannedUnits);
        List<string> typeBans = ConfigService.BannedTypes.Split(',').Select(s => s.Trim()).ToList();
        if (unitBans.Count > 0) FamiliarUnlockSystem.ExemptPrefabs = unitBans;
        if (typeBans.Count > 0) FamiliarUnlockSystem.ExemptTypes = typeBans;
    }
    public static void PrestigeBuffs()
    {
        List<int> prestigeBuffs = ParseConfigString(ConfigService.PrestigeBuffs);
        foreach (int buff in prestigeBuffs)
        {
            UpdateBuffsBufferDestroyPatch.PrestigeBuffPrefabs.Add(new PrefabGUID(buff));
        }
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
    public static void UpdateClassData(Entity character, PlayerClasses parsedClassType, Dictionary<PlayerClasses, (List<int>, List<int>)> classes, ulong steamId)
    {
        var weaponConfigEntry = ClassWeaponBloodMap[parsedClassType].Item1;
        var bloodConfigEntry = ClassWeaponBloodMap[parsedClassType].Item2;
        var classWeaponStats = ParseConfigString(weaponConfigEntry);
        var classBloodStats = ParseConfigString(bloodConfigEntry);

        classes[parsedClassType] = (classWeaponStats, classBloodStats);
        steamId.SetPlayerClasses(classes);

        FromCharacter fromCharacter = new()
        {
            Character = character,
            User = character.Read<PlayerCharacter>().UserEntity,
        };

        ApplyClassBuffs(character, steamId, fromCharacter);
    }
    public static void ApplyClassBuffs(Entity character, ulong steamId, FromCharacter fromCharacter)
    {
        var buffs = GetClassBuffs(steamId);

        if (buffs.Count == 0) return;
        int levelStep = ConfigService.MaxLevel / buffs.Count;

        int playerLevel = 0;

        if (ConfigService.LevelingSystem)
        {
            playerLevel = GetLevel(steamId);
        }
        else
        {
            Equipment equipment = character.Read<Equipment>();
            playerLevel = (int)equipment.GetFullLevel();
        }

        if (ConfigService.PrestigeSystem && steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData[PrestigeType.Experience] > 0)
        {
            playerLevel = ConfigService.MaxLevel;
        }

        int numBuffsToApply = playerLevel / levelStep;

        if (numBuffsToApply > 0 && numBuffsToApply <= buffs.Count)
        {
            for (int i = 0; i < numBuffsToApply; i++)
            {
                ApplyBuffDebugEvent applyBuffDebugEvent = new()
                {
                    BuffPrefabGUID = new(buffs[i])
                };

                if (!ServerGameManager.HasBuff(character, applyBuffDebugEvent.BuffPrefabGUID.ToIdentifier()))
                {
                    DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);

                    if (ServerGameManager.TryGetBuff(character, applyBuffDebugEvent.BuffPrefabGUID.ToIdentifier(), out Entity buff))
                    {
                        HandleBloodBuff(buff);

                        if (buff.Has<RemoveBuffOnGameplayEvent>())
                        {
                            buff.Remove<RemoveBuffOnGameplayEvent>();
                        }
                        if (buff.Has<RemoveBuffOnGameplayEventEntry>())
                        {
                            buff.Remove<RemoveBuffOnGameplayEventEntry>();
                        }
                        if (buff.Has<CreateGameplayEventsOnSpawn>())
                        {
                            buff.Remove<CreateGameplayEventsOnSpawn>();
                        }
                        if (buff.Has<GameplayEventListeners>())
                        {
                            buff.Remove<GameplayEventListeners>();
                        }
                        if (!buff.Has<Buff_Persists_Through_Death>())
                        {
                            buff.Add<Buff_Persists_Through_Death>();
                        }
                        if (buff.Has<LifeTime>())
                        {
                            LifeTime lifeTime = buff.Read<LifeTime>();
                            lifeTime.Duration = -1;
                            lifeTime.EndAction = LifeTimeEndAction.None;
                            buff.Write(lifeTime);
                        }
                    }
                }
            }
        }
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
    public static List<int> GetClassBuffs(ulong steamId)
    {
        if (steamId.TryGetPlayerClasses(out var classes) && classes.Keys.Count > 0)
        {
            var playerClass = classes.Keys.FirstOrDefault();
            return ParseConfigString(ClassPrestigeBuffsMap[playerClass]);
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
    public static bool HasClass(ulong steamId)
    {
        return steamId.TryGetPlayerClasses(out var classes) && classes.Keys.Count > 0;
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
    public static void ReplyClassBuffs(ChatCommandContext ctx, PlayerClasses playerClass)
    {
        List<int> perks = ParseConfigString(ClassPrestigeBuffsMap[playerClass]);

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
        List<int> perks = ParseConfigString(ClassSpellsMap[playerClass]);

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
    public static List<int> ParseConfigString(string configString)
    {
        if (string.IsNullOrEmpty(configString))
        {
            return [];
        }
        return configString.Split(',').Select(int.Parse).ToList();
    }
}
