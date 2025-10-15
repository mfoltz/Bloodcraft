using Bloodcraft.Services;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using ConfigInitialization = Bloodcraft.Services.ConfigService.ConfigInitialization;

namespace Bloodcraft.Tests.Support;

/// <summary>
/// Temporarily overrides <see cref="ConfigInitialization.FinalConfigValues"/> for a deterministic test
/// configuration and rebuilds the cached <see cref="Lazy{T}"/> properties exposed by
/// <see cref="ConfigService"/> when the scope exits.
/// </summary>
public sealed class ConfigOverrideScope : IDisposable
{
    static readonly MethodInfo GetConfigValueMethod = typeof(ConfigService)
        .GetMethod("GetConfigValue", BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Failed to locate ConfigService.GetConfigValue");

    static readonly Dictionary<FieldInfo, string> LazyFieldKeys = new();
    static readonly object LazyFieldKeysLock = new();
    static readonly OpCode[] OneByteOpCodes = new OpCode[0x100];
    static readonly OpCode[] TwoByteOpCodes = new OpCode[0x100];

    static ConfigOverrideScope()
    {
        foreach (var field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is OpCode opCode)
            {
                var value = (ushort)opCode.Value;
                if (value < 0x100)
                {
                    OneByteOpCodes[value] = opCode;
                }
                else if ((value & 0xFF00) == 0xFE00)
                {
                    TwoByteOpCodes[value & 0xFF] = opCode;
                }
            }
        }
    }

    readonly Dictionary<string, object> _originalValues;
    bool _disposed;

    /// <summary>
    /// Applies the supplied configuration overrides immediately and rebuilds every cached
    /// <see cref="Lazy{T}"/> accessor so subsequent reads surface the provided values.
    /// </summary>
    /// <param name="overrides">The configuration keys and values to assign for the duration of the scope.</param>
    public ConfigOverrideScope(IEnumerable<KeyValuePair<string, object>> overrides)
    {
        ArgumentNullException.ThrowIfNull(overrides);

        _originalValues = new Dictionary<string, object>(ConfigInitialization.FinalConfigValues);

        foreach (var (key, value) in overrides)
        {
            ConfigInitialization.FinalConfigValues[key] = value;
        }

        ResetLazyCaches();
    }

    /// <summary>
    /// Restores the cached configuration values and rebuilds the <see cref="Lazy{T}"/> accessors so
    /// future reads once again reflect the production configuration.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        ConfigInitialization.FinalConfigValues.Clear();
        foreach (var (key, value) in _originalValues)
        {
            ConfigInitialization.FinalConfigValues[key] = value;
        }

        ResetLazyCaches();
        _disposed = true;
    }

    static void ResetLazyCaches()
    {
        foreach (var field in typeof(ConfigService).GetFields(BindingFlags.Static | BindingFlags.NonPublic))
        {
            if (!field.FieldType.IsGenericType || field.FieldType.GetGenericTypeDefinition() != typeof(Lazy<>))
            {
                continue;
            }

            if (field.Name.Equals("_directoryPaths", StringComparison.Ordinal))
            {
                continue;
            }

            if (!TryGetConfigKey(field, out var key) || string.IsNullOrEmpty(key))
            {
                continue;
            }

            field.SetValue(null, CreateLazyInstance(field.FieldType, key));
        }
    }

    static bool TryGetConfigKey(FieldInfo field, out string key)
    {
        key = string.Empty;

        lock (LazyFieldKeysLock)
        {
            if (LazyFieldKeys.TryGetValue(field, out var existingKey))
            {
                key = existingKey;
                return true;
            }
        }

        var lazy = field.GetValue(null);
        if (lazy == null)
        {
            return false;
        }

        if (!TryExtractKeyFromLazy(lazy, out var discoveredKey) || string.IsNullOrEmpty(discoveredKey))
        {
            return false;
        }

        lock (LazyFieldKeysLock)
        {
            LazyFieldKeys[field] = discoveredKey;
        }

        key = discoveredKey;
        return true;
    }

    static bool TryExtractKeyFromLazy(object lazy, out string key)
    {
        key = string.Empty;

        var lazyType = lazy.GetType();
        var factoryField = lazyType.GetField("m_valueFactory", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? lazyType.GetField("_valueFactory", BindingFlags.Instance | BindingFlags.NonPublic);

        if (factoryField?.GetValue(lazy) is not Delegate valueFactory)
        {
            return false;
        }

        var method = valueFactory.Method;
        if (method == null)
        {
            return false;
        }

        var body = method.GetMethodBody();
        var ilBytes = body?.GetILAsByteArray();

        if (ilBytes == null)
        {
            return false;
        }

        var position = 0;
        while (position < ilBytes.Length)
        {
            var opCode = ReadOpCode(ilBytes, ref position);
            if (opCode == OpCodes.Ldstr)
            {
                var metadataToken = BitConverter.ToInt32(ilBytes, position);
                var resolved = method.Module.ResolveString(metadataToken);
                if (!string.IsNullOrEmpty(resolved))
                {
                    key = resolved;
                    return true;
                }
            }

            position += GetOperandSize(opCode, ilBytes, position);
        }

        return false;
    }

    static OpCode ReadOpCode(byte[] il, ref int position)
    {
        var code = il[position++];
        if (code != 0xFE)
        {
            return OneByteOpCodes[code];
        }

        var second = il[position++];
        return TwoByteOpCodes[second];
    }

    static int GetOperandSize(OpCode opCode, byte[] il, int position)
    {
        return opCode.OperandType switch
        {
            OperandType.InlineNone => 0,
            OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
            OperandType.InlineVar => 2,
            OperandType.InlineI or OperandType.InlineBrTarget or OperandType.InlineField or OperandType.InlineMethod or OperandType.InlineSig or OperandType.InlineString or OperandType.InlineTok or OperandType.InlineType => 4,
            OperandType.InlineR or OperandType.InlineI8 => 8,
            OperandType.ShortInlineR => 4,
            OperandType.InlineSwitch => CalculateSwitchSize(il, position),
            _ => throw new InvalidOperationException($"Unsupported operand type '{opCode.OperandType}'."),
        };
    }

    static int CalculateSwitchSize(byte[] il, int position)
    {
        var caseCount = BitConverter.ToInt32(il, position);
        return sizeof(int) + (caseCount * sizeof(int));
    }

    static object CreateLazyInstance(Type lazyType, string key)
    {
        var valueType = lazyType.GetGenericArguments()[0];
        var method = GetConfigValueMethod.MakeGenericMethod(valueType);
        var call = Expression.Call(method, Expression.Constant(key));
        var funcType = typeof(Func<>).MakeGenericType(valueType);
        var lambda = Expression.Lambda(funcType, call);
        var compiled = lambda.Compile();

        return Activator.CreateInstance(lazyType, compiled)!;
    }
}
