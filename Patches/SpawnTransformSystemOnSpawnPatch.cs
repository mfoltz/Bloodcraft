using Bloodcraft.Services;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Scripting;
using ProjectM.Shared;
using ProjectM.Shared.Systems;
using Steamworks;
using Stunlock.Core;
using System.Collections;
using System.Collections.Concurrent;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Bloodcraft.Services.DataService.FamiliarPersistence;
using static Bloodcraft.Services.DataService.PlayerDictionaries;
using static Bloodcraft.Services.PlayerService;
using User = ProjectM.Network.User;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class SpawnTransformSystemOnSpawnPatch
{
    static EntityManager EntityManager => Core.EntityManager;
    static SystemService SystemService => Core.SystemService;

    static readonly PrefabGUID manticore = new(-393555055);
    static readonly PrefabGUID dracula = new(-327335305);
    static readonly PrefabGUID monster = new(1233988687);
    static readonly PrefabGUID solarus = new(-740796338);
    static readonly PrefabGUID divineAngel = new(-1737346940);
    static readonly PrefabGUID fallenAngel = new(-76116724);

    static readonly PrefabGUID manticoreVisual = new(1670636401);
    static readonly PrefabGUID draculaVisual = new(-1923843097);
    static readonly PrefabGUID monsterVisual = new(-2067402784);
    static readonly PrefabGUID solarusVisual = new(178225731);

    public static readonly List<PrefabGUID> shardBearers = [manticore, dracula, monster, solarus];

    static readonly GameModeType GameMode = SystemService.ServerGameSettingsSystem.Settings.GameModeType;

    static readonly List<PrefabGUID> PrefabsToIgnore = // might need to add more later
    [
        new PrefabGUID(-259591573) // tomb skeleton
    ];

    public static readonly ConcurrentDictionary<ulong, List<PrefabGUID>> PlayerFamiliarBattleGroups = [];
    public static readonly ConcurrentDictionary<ulong, bool> PlayerSummoningForBattle = [];
    public static readonly ConcurrentDictionary<ulong, HashSet<Entity>> PlayerBattleFamiliars = [];
    public static readonly List<ulong> SetRotation = [];

    [HarmonyPatch(typeof(SpawnTransformSystem_OnSpawn), nameof(SpawnTransformSystem_OnSpawn.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(SpawnTransformSystem_OnSpawn __instance)
    {
        if (!Core._initialized) return;
        if (!ConfigService.FamiliarSystem && !ConfigService.EliteShardBearers) return;

        NativeArray<Entity> entities = __instance.__query_565030732_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out UnitLevel unitLevel) || !entity.TryGetComponent(out PrefabGUID prefabGUID)) continue;

                int level = unitLevel.Level._Value;
                int famKey = prefabGUID.GuidHash;
                bool summon = false;

                if (level == 1 && !PrefabsToIgnore.Contains(prefabGUID))
                {
                    Dictionary<ulong, (Entity Familiar, int FamKey)> FamiliarActives = new(familiarActives);
                    ulong steamId = FamiliarActives
                        .Where(f => f.Value.FamKey == famKey)
                        .Select(f => f.Key)
                        .FirstOrDefault(id => Misc.GetPlayerBool(id, "Binding"));

                    if (steamId == 0)
                    {
                        steamId = PlayerFamiliarBattleGroups
                            .FirstOrDefault(kvp => kvp.Value.Contains(prefabGUID)).Key;

                        if (PlayerSummoningForBattle.TryGetValue(steamId, out bool isSummoning) && isSummoning)
                        {
                            if (steamId.TryGetPlayerInfo(out PlayerInfo playerInfo))
                            {
                                User user = playerInfo.User;
                                Entity character = playerInfo.CharEntity;

                                if (FamiliarSummonSystem.HandleFamiliarForBattle(character, entity))
                                {
                                    summon = true;

                                    if (!PlayerBattleFamiliars.ContainsKey(steamId)) PlayerBattleFamiliars[steamId] = [];
                                    if (SetRotation.Contains(steamId) && entity.Has<TargetDirection>())
                                    {
                                        entity.With((ref TargetDirection targetDirection) =>
                                        {
                                            Core.Log.LogInfo($"Setting rotation for {prefabGUID.LookupName()} | {steamId}");
                                            targetDirection.AimDirection = new float3(0f, 0f, -1f);
                                        });
                                    }

                                    PlayerFamiliarBattleGroups[steamId].Remove(prefabGUID);
                                    PlayerBattleFamiliars[steamId].Add(entity);

                                    if (PlayerFamiliarBattleGroups[steamId].Count == 0)
                                    {
                                        PlayerSummoningForBattle[steamId] = false;
                                        if (SetRotation.Contains(steamId)) SetRotation.Remove(steamId);

                                        if (BattleService.Matchmaker.MatchPairs.TryGetMatch(steamId, out var matchPair))
                                        {
                                            ulong pairedId = matchPair.Item1 == steamId ? matchPair.Item2 : matchPair.Item1;

                                            if (PlayerSummoningForBattle.TryGetValue(pairedId, out bool summoning) && !summoning)
                                            {
                                                Core.StartCoroutine(BattleService.BattleStartCountdown((steamId, pairedId)));
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    DestroyUtility.Destroy(EntityManager, entity);
                                    LocalizationService.HandleServerReply(EntityManager, user, $"Failed to summon familiar...");

                                    continue;
                                }
                            }
                        }
                    }
                    else if (steamId.TryGetPlayerInfo(out PlayerInfo playerInfo))
                    {
                        User user = playerInfo.User;
                        Entity character = playerInfo.CharEntity;

                        if (FamiliarSummonSystem.HandleFamiliar(character, entity))
                        {
                            Misc.SetPlayerBool(steamId, "Binding", false);
                            string colorCode = "<color=#FF69B4>";
                            FamiliarBuffsData buffsData = FamiliarBuffsManager.LoadFamiliarBuffs(steamId);

                            if (buffsData.FamiliarBuffs.ContainsKey(famKey))
                            {
                                if (FamiliarUnlockSystem.ShinyBuffColorHexMap.TryGetValue(new(buffsData.FamiliarBuffs[famKey].First()), out var hexColor))
                                {
                                    colorCode = $"<color={hexColor}>";
                                }
                            }

                            string message = buffsData.FamiliarBuffs.ContainsKey(famKey) ? $"<color=green>{prefabGUID.GetPrefabName()}</color>{colorCode}*</color> <color=#00FFFF>bound</color>!" : $"<color=green>{prefabGUID.GetPrefabName()}</color> <color=#00FFFF>bound</color>!";
                            LocalizationService.HandleServerReply(EntityManager, user, message);
                            summon = true;
                        }
                        else
                        {
                            DestroyUtility.Destroy(EntityManager, entity);
                            LocalizationService.HandleServerReply(EntityManager, user, $"Failed to summon familiar...");

                            continue;
                        }
                    }
                }

                if (summon) continue;

                if (ConfigService.EliteShardBearers)
                {
                    if (shardBearers.Contains(prefabGUID))
                    {
                        if (prefabGUID.Equals(manticore))
                        {
                            HandleManticore(entity);
                        }
                        else if (prefabGUID.Equals(dracula))
                        {
                            HandleDracula(entity);
                        }
                        else if (prefabGUID.Equals(monster))
                        {
                            HandleMonster(entity);
                        }
                        else if (prefabGUID.Equals(solarus))
                        {
                            HandleSolarus(entity);
                        }
                    }
                    else if (prefabGUID.Equals(divineAngel))
                    {
                        HandleAngel(entity);
                    }
                    else if (prefabGUID.Equals(fallenAngel))
                    {
                        HandleFallenAngel(entity);
                    }
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
    static void HandleManticore(Entity entity)
    {
        entity.Remove<DynamicallyWeakenAttackers>();
        //if (entity.Has<MaxMinionsPerPlayerElement>()) entity.Remove<MaxMinionsPerPlayerElement>();

        var maxMinionsBuffer = entity.ReadBuffer<MaxMinionsPerPlayerElement>();
        int value = maxMinionsBuffer[0].Value;

        for (int i = 0; i < maxMinionsBuffer.Length; i++)
        {
            MaxMinionsPerPlayerElement maxMinions = maxMinionsBuffer[i];
            if (maxMinions.Value != value)
            {
                maxMinions.Value = value;
                maxMinionsBuffer[i] = maxMinions;
            }
        }

        var crowdednessDropTableBuffer = entity.ReadBuffer<CrowdednessDropTableSettingsAsset.CrowdednessSetting>();
        float dropChance = crowdednessDropTableBuffer[0].DropChance;

        for (int i = 0; i < crowdednessDropTableBuffer.Length; i++)
        {
            CrowdednessDropTableSettingsAsset.CrowdednessSetting crowdednessSetting = crowdednessDropTableBuffer[i];
            if (crowdednessSetting.DropChance != dropChance)
            {
                crowdednessSetting.DropChance = dropChance;
                crowdednessDropTableBuffer[i] = crowdednessSetting;
            }
        }

        Health health = entity.Read<Health>();
        health.MaxHealth._Value *= 5;
        health.Value = health.MaxHealth._Value;
        entity.Write(health);
        
        UnitStats unitStats = entity.Read<UnitStats>();
        unitStats.PhysicalPower._Value *= 1.5f;
        unitStats.SpellPower._Value *= 1.5f;
        entity.Write(unitStats);

        AbilityBar_Shared abilityBarShared = entity.Read<AbilityBar_Shared>();
        abilityBarShared.AttackSpeed._Value = 2f;
        abilityBarShared.PrimaryAttackSpeed._Value = 2f;
        entity.Write(abilityBarShared);

        AiMoveSpeeds aiMoveSpeeds = entity.Read<AiMoveSpeeds>();
        aiMoveSpeeds.Walk._Value = 5f;
        aiMoveSpeeds.Run._Value = 6.5f;
        entity.Write(aiMoveSpeeds);

        SetShardBearerLevel(entity);

        Buffs.HandleVisual(entity, manticoreVisual);
    }
    static void HandleMonster(Entity entity)
    {
        entity.Remove<DynamicallyWeakenAttackers>();
        if (entity.Has<MaxMinionsPerPlayerElement>()) entity.Remove<MaxMinionsPerPlayerElement>();

        var crowdednessDropTableBuffer = entity.ReadBuffer<CrowdednessDropTableSettingsAsset.CrowdednessSetting>();
        float dropChance = crowdednessDropTableBuffer[0].DropChance;

        for (int i = 0; i < crowdednessDropTableBuffer.Length; i++)
        {
            CrowdednessDropTableSettingsAsset.CrowdednessSetting crowdednessSetting = crowdednessDropTableBuffer[i];
            if (crowdednessSetting.DropChance != dropChance)
            {
                crowdednessSetting.DropChance = dropChance;
                crowdednessDropTableBuffer[i] = crowdednessSetting;
            }
        }

        Health health = entity.Read<Health>();
        health.MaxHealth._Value *= 5;
        health.Value = health.MaxHealth._Value;
        entity.Write(health);

        UnitStats unitStats = entity.Read<UnitStats>();
        unitStats.PhysicalPower._Value *= 1.5f;
        unitStats.SpellPower._Value *= 1.5f;
        entity.Write(unitStats);

        AbilityBar_Shared abilityBarShared = entity.Read<AbilityBar_Shared>();
        abilityBarShared.AttackSpeed._Value = 2f;
        abilityBarShared.PrimaryAttackSpeed._Value = 2f;
        entity.Write(abilityBarShared);

        AiMoveSpeeds aiMoveSpeeds = entity.Read<AiMoveSpeeds>();
        aiMoveSpeeds.Walk._Value = 2.5f;
        aiMoveSpeeds.Run._Value = 5.5f;
        entity.Write(aiMoveSpeeds);

        SetShardBearerLevel(entity);

        Buffs.HandleVisual(entity, monsterVisual);
    }
    static void HandleSolarus(Entity entity)
    {
        entity.Remove<DynamicallyWeakenAttackers>();
        if (entity.Has<MaxMinionsPerPlayerElement>()) entity.Remove<MaxMinionsPerPlayerElement>();

        var crowdednessDropTableBuffer = entity.ReadBuffer<CrowdednessDropTableSettingsAsset.CrowdednessSetting>();
        float dropChance = crowdednessDropTableBuffer[0].DropChance;

        for (int i = 0; i < crowdednessDropTableBuffer.Length; i++)
        {
            CrowdednessDropTableSettingsAsset.CrowdednessSetting crowdednessSetting = crowdednessDropTableBuffer[i];
            if (crowdednessSetting.DropChance != dropChance)
            {
                crowdednessSetting.DropChance = dropChance;
                crowdednessDropTableBuffer[i] = crowdednessSetting;
            }
        }

        Health health = entity.Read<Health>();
        health.MaxHealth._Value *= 5;
        health.Value = health.MaxHealth._Value;
        entity.Write(health);

        UnitStats unitStats = entity.Read<UnitStats>();
        unitStats.PhysicalPower._Value *= 1.5f;
        unitStats.SpellPower._Value *= 1.5f;
        entity.Write(unitStats);

        AbilityBar_Shared abilityBarShared = entity.Read<AbilityBar_Shared>();
        abilityBarShared.AttackSpeed._Value = 2f;
        abilityBarShared.PrimaryAttackSpeed._Value = 2f;
        entity.Write(abilityBarShared);

        AiMoveSpeeds aiMoveSpeeds = entity.Read<AiMoveSpeeds>();
        aiMoveSpeeds.Walk._Value = 4f;
        entity.Write(aiMoveSpeeds);

        SetShardBearerLevel(entity);

        Buffs.HandleVisual(entity, solarusVisual);
    }
    static void HandleAngel(Entity entity)
    {
        Health health = entity.Read<Health>();
        health.MaxHealth._Value *= 5;
        health.Value = health.MaxHealth._Value;
        entity.Write(health);

        UnitStats unitStats = entity.Read<UnitStats>();
        unitStats.PhysicalPower._Value *= 1.5f;
        unitStats.SpellPower._Value *= 1.5f;
        entity.Write(unitStats);

        AbilityBar_Shared abilityBarShared = entity.Read<AbilityBar_Shared>();
        abilityBarShared.AttackSpeed._Value = 2f;
        abilityBarShared.PrimaryAttackSpeed._Value = 2f;
        entity.Write(abilityBarShared);

        AiMoveSpeeds aiMoveSpeeds = entity.Read<AiMoveSpeeds>();
        aiMoveSpeeds.Walk._Value = 5f;
        aiMoveSpeeds.Run._Value = 7.5f;
        entity.Write(aiMoveSpeeds);
        Buffs.HandleVisual(entity, solarusVisual);
    }
    static void HandleFallenAngel(Entity entity)
    {
        Health health = entity.Read<Health>();
        health.MaxHealth._Value *= 2.5f;
        health.Value = health.MaxHealth._Value;
        entity.Write(health);
    }
    static void HandleDracula(Entity entity)
    {
        entity.Remove<DynamicallyWeakenAttackers>();
        if (entity.Has<MaxMinionsPerPlayerElement>()) entity.Remove<MaxMinionsPerPlayerElement>();

        var crowdednessDropTableBuffer = entity.ReadBuffer<CrowdednessDropTableSettingsAsset.CrowdednessSetting>();
        float dropChance = crowdednessDropTableBuffer[0].DropChance;

        for (int i = 0; i < crowdednessDropTableBuffer.Length; i++)
        {
            CrowdednessDropTableSettingsAsset.CrowdednessSetting crowdednessSetting = crowdednessDropTableBuffer[i];
            if (crowdednessSetting.DropChance != dropChance)
            {
                crowdednessSetting.DropChance = dropChance;
                crowdednessDropTableBuffer[i] = crowdednessSetting;
            }
        }

        Health health = entity.Read<Health>();
        health.MaxHealth._Value *= 5;
        health.Value = health.MaxHealth._Value;
        entity.Write(health);

        UnitStats unitStats = entity.Read<UnitStats>();
        unitStats.PhysicalPower._Value *= 1.5f;
        unitStats.SpellPower._Value *= 1.5f;
        entity.Write(unitStats);

        AbilityBar_Shared abilityBarShared = entity.Read<AbilityBar_Shared>();
        abilityBarShared.AttackSpeed._Value = 2f;
        abilityBarShared.PrimaryAttackSpeed._Value = 2f;
        entity.Write(abilityBarShared);

        AiMoveSpeeds aiMoveSpeeds = entity.Read<AiMoveSpeeds>();
        aiMoveSpeeds.Walk._Value = 2.5f;
        aiMoveSpeeds.Run._Value = 3.5f;
        aiMoveSpeeds.Circle._Value = 3.5f;
        entity.Write(aiMoveSpeeds);

        SetShardBearerLevel(entity);

        Buffs.HandleVisual(entity, draculaVisual);
    }
    static void SetShardBearerLevel(Entity shardBearer)
    {
        if (ConfigService.ShardBearerLevel > 0)
        {
            shardBearer.With((ref UnitLevel unitLevel) =>
            {
                unitLevel.Level._Value = ConfigService.ShardBearerLevel;
            });
        }
    }
}
