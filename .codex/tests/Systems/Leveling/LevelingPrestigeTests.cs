using System;
using System.Collections.Generic;
using Bloodcraft.Interfaces;
using Bloodcraft.Services;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Tests.Support;
using Bloodcraft.Utilities;
using HarmonyLib;
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
        using var componentScope = new EntityComponentScope();
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
        UnitLevel unitLevel = StructConfigurer.Configure(new UnitLevel(), traverse =>
        {
            traverse.Field("Level").Field("_Value").SetValue(currentLevel);
        });

        Health health = StructConfigurer.Configure(new Health(), traverse =>
        {
            traverse.Field("MaxHealth").Field("_Value").SetValue(maxHealth);
            traverse.Field("Value").SetValue(maxHealth);
        });

        componentScope.SetComponent(target, unitLevel);
        componentScope.SetComponent(target, health);

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

    static class StructConfigurer
    {
        public static T Configure<T>(T value, Action<Traverse> configure) where T : struct
        {
            object boxed = value;
            var traverse = Traverse.Create(boxed);
            configure(traverse);
            return (T)boxed;
        }
    }

    sealed class EntityComponentScope : IEntityComponentOverrides, IDisposable
    {
        readonly Dictionary<ComponentKey, object> components = new();
        readonly IDisposable registration;

        public EntityComponentScope()
        {
            registration = VExtensions.OverrideComponents(this);
        }

        public void SetComponent<T>(Entity entity, T component) where T : struct
        {
            components[new ComponentKey(entity, typeof(T))] = component;
        }

        public void Dispose()
        {
            components.Clear();
            registration.Dispose();
        }

        public bool TryRead<T>(Entity entity, out T value) where T : struct
        {
            if (components.TryGetValue(new ComponentKey(entity, typeof(T)), out object stored) && stored is T typed)
            {
                value = typed;
                return true;
            }

            value = default;
            return false;
        }

        public bool TryHas(Entity entity, Type componentType, out bool has)
        {
            if (components.ContainsKey(new ComponentKey(entity, componentType)))
            {
                has = true;
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
    }

    sealed class NotifyPlayerScope : IDisposable
    {
        static readonly Harmony HarmonyInstance = new("Bloodcraft.Tests.Systems.Leveling.LevelingPrestigeTests.NotifyPlayer");
        static readonly object Sync = new();
        static int scopeDepth;
        static bool patched;

        public NotifyPlayerScope()
        {
            lock (Sync)
            {
                if (!patched)
                {
                    HarmonyInstance.Patch(
                        AccessTools.Method(typeof(LevelingSystem), nameof(LevelingSystem.NotifyPlayer)),
                        prefix: new HarmonyMethod(typeof(NotifyPlayerScope), nameof(SkipNotify)));
                    patched = true;
                }

                scopeDepth++;
            }
        }

        public void Dispose()
        {
            lock (Sync)
            {
                scopeDepth--;
                if (scopeDepth == 0)
                {
                    HarmonyInstance.UnpatchSelf();
                    patched = false;
                }
            }
        }

        static bool SkipNotify()
        {
            return false;
        }
    }
}
