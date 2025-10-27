using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bloodcraft;
using Bloodcraft.Interfaces;
using Bloodcraft.Services;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Tests;
using Bloodcraft.Tests.Support;
using Bloodcraft.Utilities;
using HarmonyLib;
using Unity.Entities;
using Xunit;
using DeathEventArgs = Bloodcraft.Patches.DeathEventListenerSystemPatch.DeathEventArgs;

namespace Bloodcraft.Tests.Systems.Prestige;

public sealed class PrestigeShareTests : TestHost
{
    protected override void ResetState()
    {
        base.ResetState();
        LevelingSystem.EnablePrefabEffects = false;
    }

    [Theory]
    [MemberData(nameof(ShouldShareExperienceCases))]
    public void ShouldShareExperience_MatchesEligibilityMatrix(
        bool experienceSharingEnabled,
        bool isPvE,
        bool targetHasPrestiged,
        int levelDifference,
        int shareLevelRange,
        bool areAllied,
        bool isIgnored,
        bool expected)
    {
        bool actual = Progression.ShouldShareExperience(
            experienceSharingEnabled,
            isPvE,
            targetHasPrestiged,
            levelDifference,
            shareLevelRange,
            areAllied,
            isIgnored);

        Assert.Equal(expected, actual);
    }

    public static IEnumerable<object[]> ShouldShareExperienceCases()
    {
        yield return new object[] { false, true, true, 0, 4, true, false, false };
        yield return new object[] { true, true, false, 0, 4, true, true, false };
        yield return new object[] { true, true, true, 6, 4, false, false, true };
        yield return new object[] { true, true, false, 0, 0, false, false, true };
        yield return new object[] { true, true, false, 5, 6, true, false, true };
        yield return new object[] { true, true, false, 3, 4, false, false, true };
        yield return new object[] { true, true, false, 8, 4, false, false, false };
        yield return new object[] { true, false, false, 2, 4, true, false, true };
        yield return new object[] { true, false, true, 0, 4, false, false, false };
    }

    [Fact]
    public void ProcessExperience_HeterogeneousPrestigeSplitsExperience()
    {
        const int victimLevel = 34;
        const float victimMaxHealth = 500f;

        Entity killer = new() { Index = 100, Version = 1 };
        Entity ally = new() { Index = 101, Version = 1 };
        Entity veteran = new() { Index = 102, Version = 1 };
        Entity outsider = new() { Index = 103, Version = 1 };
        Entity target = new() { Index = 110, Version = 1 };

        using var harness = CreateHarness();
        harness.SetExperienceData(target, new LevelingSystem.ExperienceData(
            victimLevel,
            victimMaxHealth,
            isVBlood: false,
            hasWarEventTrash: false,
            isUnitSpawnerSpawned: false,
            hasDocileAggroConsumer: false));

        ParticipantState slayer = harness.RegisterParticipant(killer, 76561198000123456UL, level: 34, xpPrestige: 0);
        ParticipantState disciplined = harness.RegisterParticipant(ally, 76561198000123457UL, level: 34, xpPrestige: 2);
        ParticipantState exalted = harness.RegisterParticipant(veteran, 76561198000123458UL, level: 34, xpPrestige: 4);
        ParticipantState drifter = harness.RegisterParticipant(outsider, 76561198000123459UL, level: 34, xpPrestige: 1);

        DeathEventArgs deathEvent = harness.CreateDeathEvent(killer, target, slayer, disciplined, exalted, drifter);

        LevelingSystem.ProcessExperience(deathEvent);

        float groupMultiplier = GetGroupMultiplier();

        float expectedSlayerGain = CalculateExperienceGain(slayer.Level, slayer.PrestigeLevel, victimLevel, victimMaxHealth, groupMultiplier);
        float expectedDisciplinedGain = CalculateExperienceGain(disciplined.Level, disciplined.PrestigeLevel, victimLevel, victimMaxHealth, groupMultiplier);
        float expectedExaltedGain = CalculateExperienceGain(exalted.Level, exalted.PrestigeLevel, victimLevel, victimMaxHealth, groupMultiplier);
        float expectedDrifterGain = CalculateExperienceGain(drifter.Level, drifter.PrestigeLevel, victimLevel, victimMaxHealth, groupMultiplier);

        Assert.Equal(expectedSlayerGain, harness.GetExperienceDelta(slayer), 5);
        Assert.Equal(expectedDisciplinedGain, harness.GetExperienceDelta(disciplined), 5);
        Assert.Equal(expectedExaltedGain, harness.GetExperienceDelta(exalted), 5);
        Assert.Equal(expectedDrifterGain, harness.GetExperienceDelta(drifter), 5);

        Assert.Equal(slayer.Level, DataService.PlayerDictionaries._playerExperience[slayer.SteamId].Key);
        Assert.Equal(disciplined.Level, DataService.PlayerDictionaries._playerExperience[disciplined.SteamId].Key);
        Assert.Equal(exalted.Level, DataService.PlayerDictionaries._playerExperience[exalted.SteamId].Key);
        Assert.Equal(drifter.Level, DataService.PlayerDictionaries._playerExperience[drifter.SteamId].Key);
    }

    [Fact]
    public void ProcessExperience_MaxLevelParticipantRoutesExpertiseAwards()
    {
        const int victimLevel = 48;
        const float victimMaxHealth = 620f;

        Entity maxLevelEntity = new() { Index = 200, Version = 1 };
        Entity allyEntity = new() { Index = 201, Version = 1 };
        Entity target = new() { Index = 210, Version = 1 };

        using var harness = CreateHarness();
        harness.SetExperienceData(target, new LevelingSystem.ExperienceData(
            victimLevel,
            victimMaxHealth,
            isVBlood: false,
            hasWarEventTrash: false,
            isUnitSpawnerSpawned: false,
            hasDocileAggroConsumer: false));

        int maxLevel = ConfigService.MaxLevel;
        float maxLevelXp = Progression.ConvertLevelToXp(maxLevel);

        ParticipantState maxed = harness.RegisterParticipant(maxLevelEntity, 76561198000133456UL, maxLevel, experiencePoints: maxLevelXp, xpPrestige: 0);
        ParticipantState ally = harness.RegisterParticipant(allyEntity, 76561198000133457UL, level: Math.Max(1, maxLevel - 5), xpPrestige: 0);

        using var multiplierOverride = new StaticFieldOverride<float>(typeof(LevelingSystem), "_groupMultiplier", 1.35f);
        using var spy = new ExpertiseSpy();

        DeathEventArgs deathEvent = harness.CreateDeathEvent(maxLevelEntity, target, maxed, ally);

        LevelingSystem.ProcessExperience(deathEvent);

        Assert.Equal(1, spy.CallCount);
        Assert.Equal(multiplierOverride.Value, spy.LastMultiplier);
        Assert.Same(deathEvent, spy.LastEvent);

        Assert.Equal(0f, harness.GetExperienceDelta(maxed));

        float expectedAllyGain = CalculateExperienceGain(ally.Level, ally.PrestigeLevel, victimLevel, victimMaxHealth, multiplierOverride.Value);
        Assert.Equal(expectedAllyGain, harness.GetExperienceDelta(ally), 5);
    }

    [Fact]
    public void ProcessExperience_RestedPoolsAndGroupMultiplierCompoundGains()
    {
        const int victimLevel = 36;
        const float victimMaxHealth = 540f;

        Entity alpha = new() { Index = 300, Version = 1 };
        Entity beta = new() { Index = 301, Version = 1 };
        Entity gamma = new() { Index = 302, Version = 1 };
        Entity target = new() { Index = 310, Version = 1 };

        using var harness = CreateHarness(("RestedXPSystem", true));
        harness.SetExperienceData(target, new LevelingSystem.ExperienceData(
            victimLevel,
            victimMaxHealth,
            isVBlood: false,
            hasWarEventTrash: false,
            isUnitSpawnerSpawned: false,
            hasDocileAggroConsumer: false));

        DateTime restedTimestamp = new(2024, 01, 01, 0, 0, 0, DateTimeKind.Utc);

        ParticipantState alphaState = harness.RegisterParticipant(alpha, 76561198000143456UL, level: 35, xpPrestige: 0, restedPool: 200f, restedTimestamp: restedTimestamp);
        ParticipantState betaState = harness.RegisterParticipant(beta, 76561198000143457UL, level: 35, xpPrestige: 1, restedPool: 120f, restedTimestamp: restedTimestamp);
        ParticipantState gammaState = harness.RegisterParticipant(gamma, 76561198000143458UL, level: 35, xpPrestige: 0, restedPool: 900f, restedTimestamp: restedTimestamp);

        using var multiplierOverride = new StaticFieldOverride<float>(typeof(LevelingSystem), "_groupMultiplier", 1.5f);
        using var restedOverride = LevelingSystem.OverrideRestedXpSystem(true);

        DeathEventArgs deathEvent = harness.CreateDeathEvent(alpha, target, alphaState, betaState, gammaState);

        LevelingSystem.ProcessExperience(deathEvent);

        float groupMultiplier = multiplierOverride.Value;

        float alphaBaseGain = CalculateExperienceGain(alphaState.Level, alphaState.PrestigeLevel, victimLevel, victimMaxHealth, groupMultiplier);
        float betaBaseGain = CalculateExperienceGain(betaState.Level, betaState.PrestigeLevel, victimLevel, victimMaxHealth, groupMultiplier);
        float gammaBaseGain = CalculateExperienceGain(gammaState.Level, gammaState.PrestigeLevel, victimLevel, victimMaxHealth, groupMultiplier);

        float alphaRestedBonus = Math.Min(alphaBaseGain, alphaState.InitialRested!.Value);
        float betaRestedBonus = Math.Min(betaBaseGain, betaState.InitialRested!.Value);
        float gammaRestedBonus = Math.Min(gammaBaseGain, gammaState.InitialRested!.Value);

        Assert.Equal(alphaBaseGain + alphaRestedBonus, harness.GetExperienceDelta(alphaState), 5);
        Assert.Equal(betaBaseGain + betaRestedBonus, harness.GetExperienceDelta(betaState), 5);
        Assert.Equal(gammaBaseGain + gammaRestedBonus, harness.GetExperienceDelta(gammaState), 5);

        KeyValuePair<DateTime, float> alphaRested = harness.GetRestedEntry(alphaState)!.Value;
        KeyValuePair<DateTime, float> betaRested = harness.GetRestedEntry(betaState)!.Value;
        KeyValuePair<DateTime, float> gammaRested = harness.GetRestedEntry(gammaState)!.Value;

        Assert.Equal(restedTimestamp, alphaRested.Key);
        Assert.Equal(restedTimestamp, betaRested.Key);
        Assert.Equal(restedTimestamp, gammaRested.Key);

        Assert.Equal(alphaState.InitialRested!.Value - alphaRestedBonus, alphaRested.Value, 5);
        Assert.Equal(betaState.InitialRested!.Value - betaRestedBonus, betaRested.Value, 5);
        Assert.Equal(gammaState.InitialRested!.Value - gammaRestedBonus, gammaRested.Value, 5);
    }

    static float CalculateExperienceGain(int playerLevel, int prestigeLevel, int victimLevel, float victimMaxHealth, float groupMultiplier)
    {
        float baseExperience = ConfigService.UnitLevelingMultiplier * victimLevel;
        int additionalExperience = (int)(victimMaxHealth / 2.5f);
        float gainedExperience = baseExperience + additionalExperience;

        int levelDifference = playerLevel - victimLevel;
        if (levelDifference > 0)
        {
            float scalingFactor = MathF.Exp(-ConfigService.LevelScalingMultiplier * levelDifference);
            gainedExperience *= scalingFactor;
        }

        if (prestigeLevel > 0)
        {
            float reducer = 1f - ConfigService.LevelingPrestigeReducer * prestigeLevel;
            gainedExperience *= reducer;
        }

        return gainedExperience * groupMultiplier;
    }

    static float GetGroupMultiplier()
    {
        FieldInfo field = typeof(LevelingSystem).GetField("_groupMultiplier", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Unable to locate the LevelingSystem group multiplier.");
        return (float)field.GetValue(null)!;
    }

    PrestigeShareHarness CreateHarness(params (string Key, object Value)[] overrides)
    {
        IDisposable dataScope = CapturePlayerData();
        IDisposable persistenceScope = DataService.SuppressPersistence();
        var configScope = WithConfigOverrides(overrides);
        return new PrestigeShareHarness(dataScope, persistenceScope, configScope);
    }

    sealed class PrestigeShareHarness : IDisposable
    {
        readonly IDisposable dataScope;
        readonly IDisposable persistenceScope;
        readonly ConfigOverrideScope configScope;
        readonly ExperienceDataScope experienceScope = new();
        readonly NotifyPlayerScope notifyScope = new();
        readonly PlayerIdentityScope identityScope = new();
        bool disposed;

        public PrestigeShareHarness(IDisposable dataScope, IDisposable persistenceScope, ConfigOverrideScope configScope)
        {
            this.dataScope = dataScope;
            this.persistenceScope = persistenceScope;
            this.configScope = configScope;
        }

        public ParticipantState RegisterParticipant(
            Entity entity,
            ulong steamId,
            int level,
            float? experiencePoints = null,
            int xpPrestige = 0,
            float? restedPool = null,
            DateTime? restedTimestamp = null)
        {
            float startingExperience = experiencePoints ?? Progression.ConvertLevelToXp(level);

            identityScope.Register(entity, steamId);

            DataService.PlayerDictionaries._playerExperience[steamId] = new KeyValuePair<int, float>(level, startingExperience);

            if (xpPrestige > 0)
            {
                DataService.PlayerDictionaries._playerPrestiges[steamId] = new Dictionary<PrestigeType, int>
                {
                    [PrestigeType.Experience] = xpPrestige
                };
            }

            if (restedPool.HasValue)
            {
                DataService.PlayerDictionaries._playerRestedXP[steamId] = new KeyValuePair<DateTime, float>(
                    restedTimestamp ?? DateTime.UtcNow,
                    restedPool.Value);
            }

            return new ParticipantState(entity, steamId, level, startingExperience, xpPrestige, restedPool);
        }

        public DeathEventArgs CreateDeathEvent(Entity source, Entity target, params ParticipantState[] participants)
        {
            var participantSet = new HashSet<Entity>(participants.Select(participant => participant.Entity));
            return new DeathEventArgs
            {
                Source = source,
                Target = target,
                DeathParticipants = participantSet,
                ScrollingTextDelay = 0f
            };
        }

        public float GetExperienceDelta(ParticipantState participant)
        {
            KeyValuePair<int, float> stored = DataService.PlayerDictionaries._playerExperience[participant.SteamId];
            return stored.Value - participant.InitialExperience;
        }

        public KeyValuePair<DateTime, float>? GetRestedEntry(ParticipantState participant)
        {
            if (DataService.PlayerDictionaries._playerRestedXP.TryGetValue(participant.SteamId, out var rested))
            {
                return rested;
            }

            return null;
        }

        public void SetExperienceData(Entity target, LevelingSystem.ExperienceData data)
        {
            experienceScope.SetExperience(target, data);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            identityScope.Dispose();
            notifyScope.Dispose();
            experienceScope.Dispose();
            configScope.Dispose();
            persistenceScope.Dispose();
            dataScope.Dispose();
            disposed = true;
        }
    }

    sealed class ParticipantState
    {
        public ParticipantState(
            Entity entity,
            ulong steamId,
            int level,
            float initialExperience,
            int prestigeLevel,
            float? initialRested)
        {
            Entity = entity;
            SteamId = steamId;
            Level = level;
            InitialExperience = initialExperience;
            PrestigeLevel = prestigeLevel;
            InitialRested = initialRested;
        }

        public Entity Entity { get; }
        public ulong SteamId { get; }
        public int Level { get; }
        public float InitialExperience { get; }
        public int PrestigeLevel { get; }
        public float? InitialRested { get; }
    }

    sealed class PlayerIdentityScope : IDisposable
    {
        static readonly MethodInfo TargetMethod = AccessTools.Method(typeof(VExtensions), nameof(VExtensions.GetSteamId), new[] { typeof(Entity) })
            ?? throw new InvalidOperationException("Unable to locate VExtensions.GetSteamId");

        static PlayerIdentityScope? current;

        readonly Harmony harmony;
        readonly Dictionary<EntityKey, ulong> identities = new();

        public PlayerIdentityScope()
        {
            if (current is not null)
            {
                throw new InvalidOperationException("An identity scope is already active.");
            }

            harmony = new Harmony($"Bloodcraft.Tests.Systems.Prestige.Identity.{Guid.NewGuid()}");
            current = this;

            var prefix = typeof(PlayerIdentityScope).GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Unable to locate GetSteamId prefix");

            harmony.Patch(TargetMethod, prefix: new HarmonyMethod(prefix));
        }

        public void Register(Entity entity, ulong steamId)
        {
            identities[new EntityKey(entity)] = steamId;
        }

        public void Dispose()
        {
            harmony.Unpatch(TargetMethod, HarmonyPatchType.Prefix, harmony.Id);
            identities.Clear();
            current = null;
        }

        static bool Prefix(Entity entity, ref ulong __result)
        {
            if (current is not null && current.identities.TryGetValue(new EntityKey(entity), out ulong steamId))
            {
                __result = steamId;
                return false;
            }

            return true;
        }

        readonly record struct EntityKey(int Index, int Version)
        {
            public EntityKey(Entity entity) : this(entity.Index, entity.Version)
            {
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

    sealed class StaticFieldOverride<T> : IDisposable where T : struct
    {
        readonly FieldInfo field;
        readonly T original;
        bool disposed;

        public StaticFieldOverride(Type targetType, string fieldName, T value)
        {
            field = targetType.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException($"Field '{fieldName}' was not found on '{targetType}'.");

            object? existing = field.GetValue(null);
            if (existing is null)
            {
                throw new InvalidOperationException($"Field '{fieldName}' did not return a value.");
            }

            original = (T)existing;
            field.SetValue(null, value);
            Value = value;
        }

        public T Value { get; }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            field.SetValue(null, original);
            disposed = true;
        }
    }

    sealed class ExpertiseSpy : IDisposable
    {
        static readonly MethodInfo TargetMethod = AccessTools.Method(
                typeof(WeaponSystem),
                nameof(WeaponSystem.ProcessExpertise),
                new[] { typeof(DeathEventArgs), typeof(float) })
            ?? throw new InvalidOperationException("Unable to locate WeaponSystem.ProcessExpertise");

        static ExpertiseSpy? current;

        readonly Harmony harmony;

        public ExpertiseSpy()
        {
            if (current is not null)
            {
                throw new InvalidOperationException("An expertise spy is already active.");
            }

            harmony = new Harmony($"Bloodcraft.Tests.Systems.Prestige.ExpertiseSpy.{Guid.NewGuid()}");
            current = this;

            var prefix = typeof(ExpertiseSpy).GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Unable to locate expertise prefix");

            harmony.Patch(TargetMethod, prefix: new HarmonyMethod(prefix));
        }

        public int CallCount => ObservedMultipliers.Count;
        public float LastMultiplier => ObservedMultipliers.Last();
        public DeathEventArgs LastEvent => ObservedEvents.Last();
        public List<float> ObservedMultipliers { get; } = new();
        public List<DeathEventArgs> ObservedEvents { get; } = new();

        public void Dispose()
        {
            harmony.Unpatch(TargetMethod, HarmonyPatchType.Prefix, harmony.Id);
            ObservedMultipliers.Clear();
            ObservedEvents.Clear();
            current = null;
        }

        static bool Prefix(DeathEventArgs deathEvent, float groupMultiplier)
        {
            if (current is not null)
            {
                current.ObservedMultipliers.Add(groupMultiplier);
                current.ObservedEvents.Add(deathEvent);
            }

            return false;
        }
    }
}
