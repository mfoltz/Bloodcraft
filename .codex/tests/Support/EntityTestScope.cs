using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Bloodcraft;
using Bloodcraft.Utilities;
using Il2CppInterop.Runtime;
using HarmonyLib;
using Unity.Entities;

namespace Bloodcraft.Tests.Support;

/// <summary>
/// Provides a disposable scope that patches core entity extension methods so tests can
/// configure lightweight ECS entities without requiring the full game runtime. The scope
/// also surfaces component overrides so <see cref="VExtensions.Read{T}(Unity.Entities.Entity)"/>
/// calls observe the stubbed data.
/// </summary>
public sealed class EntityTestScope : IEntityComponentOverrides, IDisposable
{
    static readonly Harmony HarmonyInstance = new("Bloodcraft.Tests.Support.EntityTestScope");
    static readonly Dictionary<Entity, OverrideData> Overrides = new(new EntityComparer());
    static readonly ConcurrentDictionary<ulong, Entity> ActiveFamiliarStubs = new();
    static readonly object SyncRoot = new();

    static int activeCount;

    readonly Dictionary<ComponentKey, ComponentOverride> components = new();
    readonly List<Entity> registeredEntities = new();
    readonly IDisposable componentRegistration;
    bool disposed;

    /// <summary>
    /// Initializes a new entity scope and applies Harmony patches on first use.
    /// </summary>
    public EntityTestScope()
    {
        componentRegistration = VExtensions.OverrideComponents(this);

        lock (SyncRoot)
        {
            if (activeCount == 0)
            {
                Patch();
            }

            activeCount++;
        }
    }

    /// <summary>
    /// Creates a stub entity with the specified identifier and optional overrides.
    /// </summary>
    public Entity CreateEntity(
        int index,
        ulong? steamId = null,
        Entity? userEntity = null,
        bool exists = true,
        bool eligibleForCombat = true,
        int? unitLevel = null,
        bool? isVBlood = null)
    {
        var entity = new Entity { Index = index, Version = 1 };
        Configure(entity,
            exists: exists,
            steamId: steamId,
            userEntity: userEntity,
            eligibleForCombat: eligibleForCombat,
            unitLevel: unitLevel,
            isVBlood: isVBlood);
        registeredEntities.Add(entity);
        return entity;
    }

    /// <summary>
    /// Applies override values for an existing entity.
    /// </summary>
    public void Configure(
        Entity entity,
        bool? exists = null,
        ulong? steamId = null,
        Entity? userEntity = null,
        bool? eligibleForCombat = null,
        int? unitLevel = null,
        bool? isVBlood = null)
    {
        lock (SyncRoot)
        {
            OverrideData data = GetOrCreateOverride(entity);

            if (exists.HasValue)
            {
                data.Exists = exists.Value;
            }

            if (steamId.HasValue)
            {
                data.SteamId = steamId.Value;
            }

            if (userEntity.HasValue)
            {
                data.UserEntity = userEntity.Value;
            }

            if (eligibleForCombat.HasValue)
            {
                data.EligibleForCombat = eligibleForCombat.Value;
            }

            if (unitLevel.HasValue)
            {
                data.UnitLevel = unitLevel.Value;
            }

            if (isVBlood.HasValue)
            {
                data.IsVBlood = isVBlood.Value;
            }
        }
    }

    /// <summary>
    /// Registers an explicit component payload for the provided entity.
    /// </summary>
    public void SetComponent<T>(Entity entity, T component) where T : struct
    {
        components[new ComponentKey(entity, typeof(T))] = ComponentOverride.Present(component);
    }

    /// <inheritdoc />
    bool IEntityComponentOverrides.TryRead<T>(Entity entity, out T value)
    {
        if (components.TryGetValue(new ComponentKey(entity, typeof(T)), out ComponentOverride entry))
        {
            if (entry.TryRead(out value))
            {
                return true;
            }

            value = default;
            return entry.HasValue;
        }

        value = default;
        return false;
    }

    /// <inheritdoc />
    bool IEntityComponentOverrides.TryHas(Entity entity, Type componentType, out bool has)
    {
        if (components.TryGetValue(new ComponentKey(entity, componentType), out ComponentOverride entry))
        {
            has = entry.HasValue;
            return true;
        }

        has = default;
        return false;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        componentRegistration.Dispose();

        Entity[] entities;

        lock (SyncRoot)
        {
            entities = registeredEntities.ToArray();

            foreach (Entity entity in entities)
            {
                Overrides.Remove(entity);
            }

            registeredEntities.Clear();

            activeCount--;
            if (activeCount == 0)
            {
                HarmonyInstance.UnpatchSelf();
                Overrides.Clear();
                ActiveFamiliarStubs.Clear();
            }
        }

        if (components.Count > 0 && entities.Length > 0)
        {
            var keysToRemove = new List<ComponentKey>();
            foreach (KeyValuePair<ComponentKey, ComponentOverride> entry in components)
            {
                foreach (Entity entity in entities)
                {
                    if (entry.Key.Matches(entity))
                    {
                        keysToRemove.Add(entry.Key);
                        break;
                    }
                }
            }

            foreach (ComponentKey key in keysToRemove)
            {
                components.Remove(key);
            }

            components.Clear();
        }

        disposed = true;
    }

    static void Patch()
    {
        HarmonyInstance.Patch(
            AccessTools.Method(typeof(VExtensions), nameof(VExtensions.Exists)),
            prefix: new HarmonyMethod(typeof(EntityTestScope), nameof(ExistsPrefix)));

        HarmonyInstance.Patch(
            AccessTools.Method(typeof(VExtensions), nameof(VExtensions.GetSteamId)),
            prefix: new HarmonyMethod(typeof(EntityTestScope), nameof(GetSteamIdPrefix)));

        HarmonyInstance.Patch(
            AccessTools.Method(typeof(VExtensions), nameof(VExtensions.GetUserEntity)),
            prefix: new HarmonyMethod(typeof(EntityTestScope), nameof(GetUserEntityPrefix)));

        HarmonyInstance.Patch(
            AccessTools.Method(typeof(VExtensions), nameof(VExtensions.GetUnitLevel)),
            prefix: new HarmonyMethod(typeof(EntityTestScope), nameof(GetUnitLevelPrefix)));

        HarmonyInstance.Patch(
            AccessTools.Method(typeof(VExtensions), nameof(VExtensions.IsVBlood)),
            prefix: new HarmonyMethod(typeof(EntityTestScope), nameof(IsVBloodPrefix)));

        HarmonyInstance.Patch(
            AccessTools.Method(typeof(Familiars), nameof(Familiars.EligibleForCombat), new[] { typeof(Entity) }),
            prefix: new HarmonyMethod(typeof(EntityTestScope), nameof(EligibleForCombatPrefix)));

        HarmonyInstance.Patch(
            AccessTools.PropertyGetter(typeof(Entity), nameof(Entity.Null)),
            prefix: new HarmonyMethod(typeof(EntityTestScope), nameof(EntityNullPrefix)));

        HarmonyInstance.Patch(
            AccessTools.Method(
                typeof(Familiars.ActiveFamiliarManager),
                nameof(Familiars.ActiveFamiliarManager.UpdateActiveFamiliarData),
                new[] { typeof(ulong), typeof(Entity), typeof(Entity), typeof(int), typeof(bool) }),
            prefix: new HarmonyMethod(typeof(EntityTestScope), nameof(UpdateActiveFamiliarDataPrefix)));

        HarmonyInstance.Patch(
            AccessTools.Method(typeof(Familiars), nameof(Familiars.GetActiveFamiliar), new[] { typeof(Entity) }),
            prefix: new HarmonyMethod(typeof(EntityTestScope), nameof(GetActiveFamiliarPrefix)));

        ConstructorInfo? il2CppCctor = typeof(IL2CPP).TypeInitializer;
        if (il2CppCctor != null)
        {
            HarmonyInstance.Patch(il2CppCctor, prefix: new HarmonyMethod(typeof(EntityTestScope), nameof(SkipIl2CppCctor)));
        }
    }

    static bool ExistsPrefix(Entity entity, ref bool __result)
    {
        if (!IsActive)
        {
            return true;
        }

        if (TryGetOverride(entity, out OverrideData data) && data.Exists.HasValue)
        {
            __result = data.Exists.Value;
        }
        else
        {
            __result = entity != default;
        }

        return false;
    }

    static bool GetSteamIdPrefix(Entity entity, ref ulong __result)
    {
        if (!IsActive)
        {
            return true;
        }

        if (TryGetOverride(entity, out OverrideData data) && data.SteamId.HasValue)
        {
            __result = data.SteamId.Value;
        }
        else
        {
            __result = 0UL;
        }

        return false;
    }

    static bool GetUserEntityPrefix(Entity entity, ref Entity __result)
    {
        if (!IsActive)
        {
            return true;
        }

        if (TryGetOverride(entity, out OverrideData data) && data.UserEntity.HasValue)
        {
            __result = data.UserEntity.Value;
        }
        else
        {
            __result = default;
        }

        return false;
    }

    static bool GetUnitLevelPrefix(Entity entity, ref int __result)
    {
        if (!IsActive)
        {
            return true;
        }

        if (TryGetOverride(entity, out OverrideData data) && data.UnitLevel.HasValue)
        {
            __result = data.UnitLevel.Value;
        }
        else
        {
            __result = 0;
        }

        return false;
    }

    static bool IsVBloodPrefix(Entity entity, ref bool __result)
    {
        if (!IsActive)
        {
            return true;
        }

        if (TryGetOverride(entity, out OverrideData data) && data.IsVBlood.HasValue)
        {
            __result = data.IsVBlood.Value;
        }
        else
        {
            __result = false;
        }

        return false;
    }

    static bool EligibleForCombatPrefix(Entity familiar, ref bool __result)
    {
        if (!IsActive)
        {
            return true;
        }

        if (TryGetOverride(familiar, out OverrideData data) && data.EligibleForCombat.HasValue)
        {
            __result = data.EligibleForCombat.Value;
        }
        else
        {
            __result = familiar != default;
        }

        return false;
    }

    static bool TryGetOverride(Entity entity, out OverrideData data)
    {
        lock (SyncRoot)
        {
            if (Overrides.TryGetValue(entity, out OverrideData? entry) && entry is not null)
            {
                data = entry;
                return true;
            }

            data = null!;
            return false;
        }
    }

    static OverrideData GetOrCreateOverride(Entity entity)
    {
        if (!Overrides.TryGetValue(entity, out OverrideData? entry) || entry is null)
        {
            entry = new OverrideData();
            Overrides[entity] = entry;
        }

        return entry;
    }

    static bool IsActive => Volatile.Read(ref activeCount) > 0;

    sealed class OverrideData
    {
        public bool? Exists { get; set; }
        public ulong? SteamId { get; set; }
        public Entity? UserEntity { get; set; }
        public bool? EligibleForCombat { get; set; }
        public int? UnitLevel { get; set; }
        public bool? IsVBlood { get; set; }
    }

    static bool EntityNullPrefix(ref Entity __result)
    {
        if (!IsActive)
        {
            return true;
        }

        __result = default;
        return false;
    }

    static bool UpdateActiveFamiliarDataPrefix(ulong steamId, Entity familiar, Entity servant, int familiarId, bool isDismissed)
    {
        if (!IsActive)
        {
            return true;
        }

        ActiveFamiliarStubs[steamId] = familiar;
        return false;
    }

    static bool GetActiveFamiliarPrefix(Entity playerCharacter, ref Entity __result)
    {
        if (!IsActive)
        {
            return true;
        }

        ulong steamId = playerCharacter.GetSteamId();
        if (ActiveFamiliarStubs.TryGetValue(steamId, out Entity familiar))
        {
            __result = familiar;
            return false;
        }

        return true;
    }

    static bool SkipIl2CppCctor()
    {
        return !IsActive;
    }

    internal static void RemoveFamiliarStub(ulong steamId)
    {
        ActiveFamiliarStubs.TryRemove(steamId, out _);
    }

    readonly record struct ComponentKey(int Index, int Version, Type ComponentType)
    {
        public ComponentKey(Entity entity, Type componentType)
            : this(entity.Index, entity.Version, componentType)
        {
        }

        public bool Matches(Entity entity)
        {
            return Index == entity.Index && Version == entity.Version;
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

    sealed class EntityComparer : IEqualityComparer<Entity>
    {
        public bool Equals(Entity x, Entity y)
        {
            return x.Index == y.Index && x.Version == y.Version;
        }

        public int GetHashCode(Entity obj)
        {
            return HashCode.Combine(obj.Index, obj.Version);
        }
    }
}
