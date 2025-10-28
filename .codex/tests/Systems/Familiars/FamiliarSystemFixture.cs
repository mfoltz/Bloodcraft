using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Utilities;
using HarmonyLib;
using Unity.Entities;
using FamiliarsUtilities = Bloodcraft.Utilities.Familiars;

namespace Bloodcraft.Tests.Systems.Familiars;

public sealed class FamiliarSystemFixture : IDisposable
{
    const string HarmonyId = "Bloodcraft.Tests.Systems.Familiars.FamiliarSystemFixture";

    static readonly ThreadLocal<List<InvocationRecord<ModifyUnitStatsCall>>> modifyUnitStatsCalls = new(() => new List<InvocationRecord<ModifyUnitStatsCall>>());
    static readonly ThreadLocal<List<InvocationRecord<ModifyBloodSourceCall>>> modifyBloodSourceCalls = new(() => new List<InvocationRecord<ModifyBloodSourceCall>>());
    static readonly ThreadLocal<List<InvocationRecord<RefreshStatsCall>>> refreshStatsCalls = new(() => new List<InvocationRecord<RefreshStatsCall>>());

    static readonly Harmony HarmonyInstance = new(HarmonyId);
    static bool isPatched;

    public FamiliarSystemFixture()
    {
        EnsurePatched();
        ClearLogs();
    }

    public static IReadOnlyList<InvocationRecord<ModifyUnitStatsCall>> ModifyUnitStatsCalls => GetModifyUnitStatsLog();

    public static IReadOnlyList<InvocationRecord<ModifyBloodSourceCall>> ModifyBloodSourceCalls => GetModifyBloodSourceLog();

    public static IReadOnlyList<InvocationRecord<RefreshStatsCall>> RefreshStatsCalls => GetRefreshStatsLog();

    public static void ClearLogs()
    {
        GetModifyUnitStatsLog().Clear();
        GetModifyBloodSourceLog().Clear();
        GetRefreshStatsLog().Clear();
    }

    void EnsurePatched()
    {
        if (isPatched)
        {
            return;
        }

        PatchTypeInitializer(typeof(FamiliarBindingSystem));
        PatchTypeInitializer(typeof(Buffs));
        PatchTypeInitializer(typeof(FamiliarsUtilities));
        PatchTypeInitializer(typeof(FamiliarLevelingSystem));

        Patch(
            AccessTools.Method(typeof(FamiliarBindingSystem), nameof(FamiliarBindingSystem.ModifyUnitStats)),
            AccessTools.Method(typeof(FamiliarSystemFixture), nameof(LogModifyUnitStatsPrefix)),
            AccessTools.Method(typeof(FamiliarSystemFixture), nameof(LogModifyUnitStatsPostfix)));

        Patch(
            AccessTools.Method(typeof(FamiliarBindingSystem), nameof(FamiliarBindingSystem.ModifyBloodSource)),
            AccessTools.Method(typeof(FamiliarSystemFixture), nameof(LogModifyBloodSourcePrefix)),
            AccessTools.Method(typeof(FamiliarSystemFixture), nameof(LogModifyBloodSourcePostfix)));

        Patch(
            AccessTools.Method(typeof(Buffs), nameof(Buffs.RefreshStats)),
            AccessTools.Method(typeof(FamiliarSystemFixture), nameof(LogRefreshStatsPrefix)),
            AccessTools.Method(typeof(FamiliarSystemFixture), nameof(LogRefreshStatsPostfix)));

        isPatched = true;
    }

    static void Patch(MethodBase? target, MethodInfo prefix, MethodInfo postfix)
    {
        if (target == null)
        {
            throw new InvalidOperationException("Target method for Harmony patch could not be located.");
        }

        HarmonyInstance.Patch(target, prefix: new HarmonyMethod(prefix), postfix: new HarmonyMethod(postfix));
    }

    static void PatchTypeInitializer(Type type)
    {
        ConstructorInfo? typeInitializer = type.TypeInitializer;
        if (typeInitializer == null)
        {
            return;
        }

        HarmonyInstance.Patch(typeInitializer, prefix: new HarmonyMethod(typeof(FamiliarSystemFixture).GetMethod(nameof(SkipTypeInitializer), BindingFlags.Static | BindingFlags.NonPublic)));
    }

    static bool SkipTypeInitializer()
    {
        return false;
    }

    public void Dispose()
    {
        if (isPatched)
        {
            HarmonyInstance.UnpatchSelf();
            isPatched = false;
        }

        ClearLogs();
    }

    static bool LogModifyUnitStatsPrefix(Entity familiar, int level, ulong steamId, int famKey, bool battle)
    {
        GetModifyUnitStatsLog().Add(new InvocationRecord<ModifyUnitStatsCall>(InvocationStage.Prefix, new ModifyUnitStatsCall(familiar, level, steamId, famKey, battle)));
        return false;
    }

    static void LogModifyUnitStatsPostfix(Entity familiar, int level, ulong steamId, int famKey, bool battle)
    {
        GetModifyUnitStatsLog().Add(new InvocationRecord<ModifyUnitStatsCall>(InvocationStage.Postfix, new ModifyUnitStatsCall(familiar, level, steamId, famKey, battle)));
    }

    static bool LogModifyBloodSourcePrefix(Entity familiar, int level)
    {
        GetModifyBloodSourceLog().Add(new InvocationRecord<ModifyBloodSourceCall>(InvocationStage.Prefix, new ModifyBloodSourceCall(familiar, level)));
        return false;
    }

    static void LogModifyBloodSourcePostfix(Entity familiar, int level)
    {
        GetModifyBloodSourceLog().Add(new InvocationRecord<ModifyBloodSourceCall>(InvocationStage.Postfix, new ModifyBloodSourceCall(familiar, level)));
    }

    static bool LogRefreshStatsPrefix(Entity entity)
    {
        GetRefreshStatsLog().Add(new InvocationRecord<RefreshStatsCall>(InvocationStage.Prefix, new RefreshStatsCall(entity)));
        return false;
    }

    static void LogRefreshStatsPostfix(Entity entity)
    {
        GetRefreshStatsLog().Add(new InvocationRecord<RefreshStatsCall>(InvocationStage.Postfix, new RefreshStatsCall(entity)));
    }

    static List<InvocationRecord<ModifyUnitStatsCall>> GetModifyUnitStatsLog()
    {
        return EnsureValue(modifyUnitStatsCalls);
    }

    static List<InvocationRecord<ModifyBloodSourceCall>> GetModifyBloodSourceLog()
    {
        return EnsureValue(modifyBloodSourceCalls);
    }

    static List<InvocationRecord<RefreshStatsCall>> GetRefreshStatsLog()
    {
        return EnsureValue(refreshStatsCalls);
    }

    static List<T> EnsureValue<T>(ThreadLocal<List<T>> storage)
    {
        var value = storage.Value;
        if (value == null)
        {
            value = new List<T>();
            storage.Value = value;
        }

        return value;
    }

    public readonly record struct ModifyUnitStatsCall(Entity Familiar, int Level, ulong SteamId, int FamiliarKey, bool IsBattle);

    public readonly record struct ModifyBloodSourceCall(Entity Familiar, int Level);

    public readonly record struct RefreshStatsCall(Entity Familiar);

    public readonly record struct InvocationRecord<T>(InvocationStage Stage, T Payload);

    public enum InvocationStage
    {
        Prefix,
        Postfix
    }
}
