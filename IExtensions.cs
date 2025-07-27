using System.Collections;
using UnityEngine;

namespace Bloodcraft;
internal static class IExtensions
{
    static readonly System.Random _random = new();
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
    public static T DrawRandom<T>(this IList<T> list)
    {
        int index = _random.Next(list.Count);

        if (list.IsIndexWithinRange(index))
            return list[index];

        return default;
    }
    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = _random.Next(n + 1);
            (list[n], list[k]) = (list[k], list[n]);
        }
    }
    public static bool Equals<T>(this T value, params T[] options)
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
    public static void Run(this IEnumerator routine)
    {
        Core.StartCoroutine(routine);
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
