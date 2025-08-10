using Bloodcraft.Resources;
using Bloodcraft.Services;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Shared.Systems;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class SpawnTransformSystemOnSpawnPatch
{
    static EntityManager EntityManager => Core.EntityManager;
    static SystemService SystemService => Core.SystemService;

    static readonly GameModeType _gameMode = SystemService.ServerGameSettingsSystem.Settings.GameModeType;

    static readonly bool _eliteShardBearers = ConfigService.EliteShardBearers;
    static readonly bool _familiars = ConfigService.FamiliarSystem;
    static readonly int _shardBearerLevel = ConfigService.ShardBearerLevel;

    static readonly PrefabGUID _manticore = new(-393555055);
    static readonly PrefabGUID _dracula = new(-327335305);
    static readonly PrefabGUID _monster = new(1233988687);
    static readonly PrefabGUID _solarus = new(-740796338);
    static readonly PrefabGUID _megara = new(591725925);
    static readonly PrefabGUID _divineAngel = new(-1737346940);
    static readonly PrefabGUID _fallenAngel = new(-76116724);

    static readonly PrefabGUID _manticoreVisual = new(1670636401);
    static readonly PrefabGUID _draculaVisual = PrefabGUIDs.AB_Shapeshift_BloodHunger_BloodSight_Buff;
    static readonly PrefabGUID _monsterVisual = new(-2067402784);
    static readonly PrefabGUID _solarusVisual = new(178225731);
    static readonly PrefabGUID _megaraVisual = new(-2104035188); // educated guess this is the purple aura for corrupted blood units

    static readonly List<PrefabGUID> _variantBuffs = // #soonTM
    [
        new(-429891372), // AB_Chaos_PowerSurge_Buff
    ];

    [HarmonyPatch(typeof(SpawnTransformSystem_OnSpawn), nameof(SpawnTransformSystem_OnSpawn.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(SpawnTransformSystem_OnSpawn __instance)
    {
        if (!Core.IsReady) return;
        else if (!_eliteShardBearers) return;

        NativeArray<Entity> entities = __instance.__query_565030732_0.ToEntityArray(Allocator.Temp);

        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out PrefabGUID prefabGuid)) continue;
                else if (entity.IsPlayerOwned()) continue;

                int guidHash = prefabGuid.GuidHash;

                switch (guidHash)
                {
                    case -393555055: // Manticore
                        HandleManticore(entity);
                        break;
                    case -327335305: // Dracula
                        HandleDracula(entity);
                        break;
                    case 1233988687: // Monster
                        HandleMonster(entity);
                        break;
                    case -740796338: // Solarus
                        HandleSolarus(entity);
                        break;
                    case 591725925: // Megara
                        HandleMegara(entity);
                        break;
                    case -1737346940: // Divine Angel
                        HandleDivineAngel(entity);
                        break;
                    case -76116724: // Fallen Angel
                        HandleFallenAngel(entity);
                        break;
                    default:
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogError($"SpawnTransformSystem error: {ex}");
        }
        finally
        {
            entities.Dispose();
        }
    }
    static void HandleManticore(Entity entity)
    {
        entity.Remove<DynamicallyWeakenAttackers>();

        SetLevel(entity);
        SetAttackSpeed(entity);
        SetHealth(entity);
        SetPower(entity);
        SetMoveSpeed(entity, 5f, 6.5f);

        Buffs.HandleShinyBuff(entity, _manticoreVisual);
    }
    static void HandleMonster(Entity entity)
    {
        entity.Remove<DynamicallyWeakenAttackers>();

        SetLevel(entity);
        SetAttackSpeed(entity);
        SetHealth(entity);
        SetPower(entity);
        SetMoveSpeed(entity, 2.5f, 5.5f);

        Buffs.HandleShinyBuff(entity, _monsterVisual);
    }
    static void HandleSolarus(Entity entity)
    {
        entity.Remove<DynamicallyWeakenAttackers>();

        SetLevel(entity);
        SetAttackSpeed(entity);
        SetHealth(entity);
        SetPower(entity);
        SetMoveSpeed(entity, 4f);

        Buffs.HandleShinyBuff(entity, _solarusVisual);
    }
    static void HandleDracula(Entity entity)
    {
        entity.Remove<DynamicallyWeakenAttackers>();

        SetLevel(entity);
        SetAttackSpeed(entity);
        SetHealth(entity);
        SetPower(entity);
        SetMoveSpeed(entity, 2.5f, 3.5f);

        Buffs.HandleShinyBuff(entity, _draculaVisual);
    }
    static void HandleMegara(Entity entity)
    {
        entity.Remove<DynamicallyWeakenAttackers>();

        SetLevel(entity);
        SetAttackSpeed(entity);
        SetHealth(entity);
        SetPower(entity);
        // SetMoveSpeed(entity, 2.5f, 3.5f); // should probably wait till fighting her before tuning >_>

        Buffs.HandleShinyBuff(entity, _megaraVisual);
    }
    static void HandleDivineAngel(Entity entity)
    {
        SetAttackSpeed(entity);
        SetHealth(entity);
        SetPower(entity);
        SetMoveSpeed(entity, 5f, 7.5f);

        Buffs.HandleShinyBuff(entity, _solarusVisual);
    }
    static void HandleFallenAngel(Entity entity)
    {
        SetHealth(entity);
    }
    static void SetLevel(Entity entity)
    {
        if (_shardBearerLevel > 0)
        {
            entity.With((ref UnitLevel unitLevel) => unitLevel.Level._Value = _shardBearerLevel);
        }
    }
    static void SetAttackSpeed(Entity entity)
    {
        entity.With((ref AbilityBar_Shared abilityBarShared) =>
        {
            abilityBarShared.AbilityAttackSpeed._Value = 2f;
            abilityBarShared.PrimaryAttackSpeed._Value = 2f;
        });
    }
    static void SetHealth(Entity entity)
    {
        entity.With((ref Health health) =>
        {
            health.MaxHealth._Value *= 5;
            health.Value = health.MaxHealth._Value;
        });
    }
    static void SetPower(Entity entity)
    {
        entity.With((ref UnitStats unitStats) =>
        {
            unitStats.PhysicalPower._Value *= 1.5f;
            unitStats.SpellPower._Value *= 1.5f;
        });
    }
    static void SetMoveSpeed(Entity entity, float walk = float.NaN, float run = float.NaN)
    {
        entity.With((ref AiMoveSpeeds aiMoveSpeeds) =>
        {
            if (!float.IsNaN(walk)) aiMoveSpeeds.Walk._Value = walk;
            if (!float.IsNaN(run)) aiMoveSpeeds.Run._Value = run;
        });
    }
}
