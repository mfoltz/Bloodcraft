using System.Collections.Concurrent;
using System.Reflection;
using PlayerDictionaries = Bloodcraft.Services.DataService.PlayerDictionaries;

namespace Bloodcraft.Tests.Support;

/// <summary>
/// Provides a scoped snapshot of the <see cref="PlayerDictionaries"/> static caches so tests can
/// safely mutate them without leaking state to other test cases.
/// </summary>
public sealed class DataStateScope : IDisposable
{
    readonly List<(FieldInfo Field, object Snapshot)> _snapshots;

    /// <summary>
    /// Captures the current contents of each <see cref="ConcurrentDictionary{TKey, TValue}"/> stored in
    /// <see cref="PlayerDictionaries"/>. The dictionaries are cloned immediately to ensure future
    /// mutations to the shared instances do not affect the recorded baseline.
    /// </summary>
    public DataStateScope()
    {
        _snapshots = typeof(PlayerDictionaries)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(ConcurrentDictionary<,>))
            .Select(field => (Field: field, Snapshot: CloneDictionary(field)))
            .ToList();
    }

    /// <summary>
    /// Restores every tracked dictionary to the state that was captured when the scope was constructed.
    /// </summary>
    public void Dispose()
    {
        foreach (var (field, snapshot) in _snapshots)
        {
            field.SetValue(null, snapshot);
        }
    }

    static object CloneDictionary(FieldInfo field)
    {
        var current = field.GetValue(null);
        if (current is null)
        {
            return Activator.CreateInstance(field.FieldType)!;
        }

        return Activator.CreateInstance(field.FieldType, current)!;
    }
}
