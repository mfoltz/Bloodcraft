using Bloodcraft.Services;
using System.Linq.Expressions;
using System.Reflection;
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

            var key = ToConfigKey(field.Name);
            if (string.IsNullOrEmpty(key))
            {
                continue;
            }

            field.SetValue(null, CreateLazyInstance(field.FieldType, key));
        }
    }

    static string ToConfigKey(string fieldName)
    {
        if (string.IsNullOrEmpty(fieldName) || fieldName[0] != '_')
        {
            return string.Empty;
        }

        var trimmed = fieldName.TrimStart('_');
        return string.Create(trimmed.Length, trimmed, (span, value) =>
        {
            span[0] = char.ToUpperInvariant(value[0]);
            value.AsSpan(1).CopyTo(span[1..]);
        });
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
