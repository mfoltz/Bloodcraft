using Bloodcraft.Utilities;
using System.Collections;
using UnityEngine;

namespace Bloodcraft;
internal static class IExtensions
{
    public static Dictionary<TValue, TKey> Reverse<TKey, TValue>(
        this IDictionary<TKey, TValue> source)
    {
        var reversed = new Dictionary<TValue, TKey>();

        foreach (var kvp in source)
        {
            reversed[kvp.Value] = kvp.Key;
        }

        return reversed;
    }
    public static Dictionary<TValue, TKey> ReverseIl2CppDictionary<TKey, TValue>(
        this Il2CppSystem.Collections.Generic.Dictionary<TKey, TValue> source)
    {
        var reversed = new Dictionary<TValue, TKey>();

        if (source == null) return reversed;

        foreach (var kvp in source)
        {
            if (reversed.ContainsKey(kvp.Value))
            {
                continue;
            }

            reversed[kvp.Value] = kvp.Key;
        }

        return reversed;
    }
    public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
    {
        foreach (var item in collection)
        {
            action(item);
        }
    }
    public static bool IsIndexWithinRange<T>(this IList<T> list, int index)
    {
        return index >= 0 && index < list.Count;
    }
    public static bool ContainsAll(this string stringChars, List<string> strings)
    {
        foreach (string str in strings)
        {
            if (!stringChars.Contains(str, StringComparison.CurrentCultureIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
    public static T DrawRandom<T>(this IList<T> list)
    {
        if (list?.Any() != true)
            return default;

        var random = new System.Random((int)Misc.GetRandomSeed());
        int index = random.Next(list.Count);

        return list.IsIndexWithinRange(index) ? list[index] : default;
    }
    public static IEnumerable<IReadOnlyList<T>> Batch<T>(this IReadOnlyList<T> source, int size)
    {
        if (size <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size));
        }

        var list = source as List<T> ?? throw new ArgumentException("Source must be a List<T>.", nameof(source));

        for (var i = 0; i < list.Count; i += size)
        {
            yield return list.GetRange(i, Math.Min(size, list.Count - i));
        }
    }
    public static bool ContainsAny(this string stringChars, List<string> strings, StringComparison stringComparison = StringComparison.CurrentCultureIgnoreCase)
    {
        foreach (string str in strings)
        {
            if (stringChars.Contains(str, stringComparison))
            {
                return true;
            }
        }

        return false;
    }
    public static void Shuffle<T>(this IList<T> list)
    {
        var random = new System.Random((int)Misc.GetRandomSeed());
        int n = list.Count;

        while (n > 1)
        {
            n--;
            int k = random.Next(n + 1);
            (list[n], list[k]) = (list[k], list[n]);
        }
    }
    public static bool Equals<T>(this T value, params T[] options) where T : unmanaged
    {
        foreach (var option in options)
        {
            if (value.Equals(option)) return true;
        }

        return false;
    }
    public static bool Contains<T>(this IEnumerable<T> source, params T[] values)
    {
        foreach (var value in values)
        {
            if (Enumerable.Contains(source, value)) return true;
        }

        return false;
    }
    public static void Run(this IEnumerator routine, float delay = 0f)
    {
        if (delay > 0f)
            Core.StartCoroutine(Delay(routine, delay));
        else
            Core.StartCoroutine(routine);
    }
    public static IEnumerator Delay(IEnumerator routine, float delay)
    {
        yield return new WaitForSeconds(delay);
        routine.Run();
    }
    public static Coroutine Start(this IEnumerator routine)
    {
        return Core.StartCoroutine(routine);
    }
    public static void Stop(this Coroutine coroutine)
    {
        Core.StopCoroutine(coroutine);
    }
}
