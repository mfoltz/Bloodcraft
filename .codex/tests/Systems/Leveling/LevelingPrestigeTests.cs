using System;
using System.Collections.Generic;
using Bloodcraft.Interfaces;
using Bloodcraft.Services;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Tests.Support;
using Bloodcraft.Utilities;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;
using Xunit;

namespace Bloodcraft.Tests.Systems.Leveling;

public sealed class LevelingPrestigeTests : TestHost
{
    const ulong SteamId = 76561198000042420UL;

    protected override void ResetState()
    {
        base.ResetState();
        LevelingSystem.EnablePrefabEffects = false;
    }

    [Fact]
    public void ProcessExperienceGain_AppliesPrestigeRestedAndGroupAdjustments()
    {
        using var dataScope = CapturePlayerData();
        using var persistence = DataService.SuppressPersistence();
        using var config = WithConfigOverrides(("RestedXPSystem", true));
        using var componentScope = new EntityComponentScope();
        using var experienceScope = new ExperienceDataScope();
        using var restedOverride = LevelingSystem.OverrideRestedXpSystem(true);
        using var notifyScope = new NotifyPlayerScope();

        var player = new Entity { Index = 1, Version = 1 };
        var target = new Entity { Index = 2, Version = 1 };

        const int currentLevel = 10;
        float currentXp = Progression.ConvertLevelToXp(currentLevel);
        DataService.PlayerDictionaries._playerExperience[SteamId] = new KeyValuePair<int, float>(currentLevel, currentXp);

        DateTime restedTimestamp = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        float restedPool = 400f;
        DataService.PlayerDictionaries._playerRestedXP[SteamId] = new KeyValuePair<DateTime, float>(restedTimestamp, restedPool);

        int prestigeLevel = 3;
        DataService.PlayerDictionaries._playerPrestiges[SteamId] = new Dictionary<PrestigeType, int>
        {
            [PrestigeType.Experience] = prestigeLevel
        };

        const float maxHealth = 250f;

        experienceScope.SetExperience(target, new LevelingSystem.ExperienceData(
            currentLevel,
            maxHealth,
            isVBlood: false,
            hasWarEventTrash: false,
            isUnitSpawnerSpawned: false,
            hasDocileAggroConsumer: false));

        const float groupMultiplier = 1.6f;

        LevelingSystem.ProcessExperienceGain(player, target, SteamId, currentLevel, delay: 0f, groupMultiplier);

        KeyValuePair<int, float> storedExperience = DataService.PlayerDictionaries._playerExperience[SteamId];
        KeyValuePair<DateTime, float> storedRested = DataService.PlayerDictionaries._playerRestedXP[SteamId];


        float baseExperience = currentLevel * ConfigService.UnitLevelingMultiplier;
        float additionalExperience = maxHealth / 2.5f;
        float prestigeReducer = 1f - ConfigService.LevelingPrestigeReducer * prestigeLevel;
        float afterPrestige = (baseExperience + additionalExperience) * prestigeReducer;
        float afterGroup = afterPrestige * groupMultiplier;
        float restedBonus = Math.Min(afterGroup, restedPool);
        float expectedGain = afterGroup + restedBonus;
        float expectedTotal = currentXp + expectedGain;

        Assert.Equal(currentLevel, storedExperience.Key);
        Assert.Equal(expectedTotal, storedExperience.Value, 5);
        Assert.Equal(restedTimestamp, storedRested.Key);
        Assert.Equal(restedPool - restedBonus, storedRested.Value, 5);
    }
    sealed class EntityComponentScope : IEntityComponentOverrides, IDisposable
    {
        readonly Dictionary<ComponentKey, ComponentOverride> components = new();
        readonly IDisposable registration;

        public EntityComponentScope()
        {
            registration = VExtensions.OverrideComponents(this);
        }

        public void SetComponent<T>(Entity entity, T component) where T : struct
        {
            components[new ComponentKey(entity, typeof(T))] = ComponentOverride.Present(component);
        }

        public void Dispose()
        {
            components.Clear();
            registration.Dispose();
        }

        public bool TryRead<T>(Entity entity, out T value) where T : struct
        {
            if (components.TryGetValue(new ComponentKey(entity, typeof(T)), out ComponentOverride entry))
            {
                if (entry.TryRead(out value))
                {
                    return true;
                }

                value = default;
                return true;
            }

            value = default;
            return false;
        }

        public bool TryHas(Entity entity, Type componentType, out bool has)
        {
            if (components.TryGetValue(new ComponentKey(entity, componentType), out ComponentOverride entry))
            {
                has = entry.HasValue;
                return true;
            }

            has = default;
            return false;
        }

        readonly record struct ComponentKey(int Index, int Version, Type ComponentType)
        {
            public ComponentKey(Entity entity, Type componentType) : this(entity.Index, entity.Version, componentType)
            {
            }
        }

        readonly struct ComponentOverride
        {
            public bool HasValue { get; }
            public object? Value { get; }

            ComponentOverride(bool hasValue, object? value)
            {
                HasValue = hasValue;
                Value = value;
            }

            public static ComponentOverride Present<T>(T value) where T : struct
            {
                return new ComponentOverride(true, value);
            }

            public bool TryRead<T>(out T value) where T : struct
            {
                if (HasValue && Value is T typed)
                {
                    value = typed;
                    return true;
                }

                value = default;
                return false;
            }
        }
    }

    sealed class ExperienceDataScope : LevelingSystem.IExperienceDataOverride, IDisposable
    {
        readonly Dictionary<EntityKey, LevelingSystem.ExperienceData> overrides = new();
        readonly IDisposable registration;

        public ExperienceDataScope()
        {
            registration = LevelingSystem.OverrideExperienceData(this);
        }

        public void SetExperience(Entity entity, LevelingSystem.ExperienceData data)
        {
            overrides[new EntityKey(entity)] = data;
        }

        public bool TryGetExperienceData(Entity target, out LevelingSystem.ExperienceData data)
        {
            return overrides.TryGetValue(new EntityKey(target), out data);
        }

        public void Dispose()
        {
            overrides.Clear();
            registration.Dispose();
        }

        readonly record struct EntityKey(int Index, int Version)
        {
            public EntityKey(Entity entity) : this(entity.Index, entity.Version)
            {
            }
        }
    }

    sealed class NotifyPlayerScope : IDisposable
    {
        readonly IDisposable registration;

        public NotifyPlayerScope()
        {
            registration = LevelingSystem.OverrideNotifyPlayer((_, _, _, _, _, _, _) => { });
        }

        public void Dispose()
        {
            registration.Dispose();
        }
    }
}
