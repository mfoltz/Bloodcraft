using Bloodcraft.Patches;
using Bloodcraft.Services;
using Bloodcraft.Systems.Leveling;
using ProjectM;
using ProjectM.Gameplay.Scripting;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Entities;
using static Bloodcraft.Systems.Leveling.LevelingSystem;
using static Bloodcraft.Utilities.ClassUtilities;

namespace Bloodcraft.Utilities;
internal static class BuffUtilities
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static DebugEventsSystem DebugEventsSystem => SystemService.DebugEventsSystem;

    const float CaptureTime = 6.25f; // with padding
    public static void ApplyBuff(PrefabGUID buffPrefab, Entity target)
    {
        ApplyBuffDebugEvent applyBuffDebugEvent = new()
        {
            BuffPrefabGUID = buffPrefab
        };

        FromCharacter fromCharacter = new()
        {
            Character = target,
            User = target
        };

        if (!ServerGameManager.HasBuff(target, buffPrefab.ToIdentifier())) DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
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
    public static void PrestigeBuffs()
    {
        List<int> prestigeBuffs = ConfigUtilities.ParseConfigString(ConfigService.PrestigeBuffs);
        foreach (int buff in prestigeBuffs)
        {
            UpdateBuffsBufferDestroyPatch.PrestigeBuffPrefabs.Add(new PrefabGUID(buff));
        }
    }
    public static void HandleImmaterialBuff(Entity entity)
    {
        if (entity.GetBuffTarget().GetOwner().IsPlayer())
        {
            DestroyUtility.Destroy(EntityManager, entity, DestroyDebugReason.TryRemoveBuff);
            return;
        }

        if (entity.Has<ModifyMovementSpeedBuff>()) entity.Remove<ModifyMovementSpeedBuff>();
        entity.With((ref LifeTime lifeTime) =>
        {
            lifeTime.Duration = CaptureTime;
            lifeTime.EndAction = LifeTimeEndAction.Destroy;
        });
    }
    public static void HandleCaptureBuff(Entity entity)
    {
        if (entity.GetBuffTarget().GetOwner().IsPlayer())
        {
            DestroyUtility.Destroy(EntityManager, entity, DestroyDebugReason.TryRemoveBuff);
            return;
        }

        if (entity.Has<AbilityProjectileFanOnGameplayEvent_DataServer>()) entity.Remove<AbilityProjectileFanOnGameplayEvent_DataServer>();
        if (entity.Has<LifeTime>()) entity.Write(new LifeTime { Duration = CaptureTime, EndAction = LifeTimeEndAction.Destroy });

        entity.With((ref AmplifyBuff amplifyBuff) =>
        {
            amplifyBuff.AmplifyModifier = -1f;
        });

        ModifyMovementSpeedBuff modifyMovementSpeedBuff = new()
        {
            MultiplyAdd = false,
            MoveSpeed = 0f
        };
        entity.Add<ModifyMovementSpeedBuff>();
        entity.Write(modifyMovementSpeedBuff);

        entity.Add<HideTargetHUD>();
    }
    public static void HandleBreakBuff(Entity entity)
    {
        entity.With((ref LifeTime lifeTime) =>
        {
            lifeTime.Duration = 1.5f;
            lifeTime.EndAction = LifeTimeEndAction.Destroy;
        });

        entity.Add<BlockFeedBuff>();

        if (entity.Has<HealOnGameplayEvent>()) entity.Remove<HealOnGameplayEvent>();
    }
}
