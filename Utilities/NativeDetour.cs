using BepInEx.Unity.IL2CPP.Hook;
using HarmonyLib;
using System.Reflection;

namespace Bloodcraft.Utilities;
internal static class NativeDetour
{
    public static INativeDetour Create<T>(Type type, string innerTypeName, string methodName, T to, out T original) where T : Delegate
    {
        return Create(GetInnerType(type, innerTypeName), methodName, to, out original);
    }
    public static INativeDetour Create<T>(Type type, string methodName, T to, out T original) where T : Delegate
    {
        return Create(type.GetMethod(methodName, AccessTools.all), to, out original);
    }
    static INativeDetour Create<T>(MethodInfo method, T to, out T original) where T : Delegate
    {
        var address = MethodResolver.ResolveFromMethodInfo(method);
        return INativeDetour.CreateAndApply(address, to, out original);
    }
    static Type GetInnerType(Type type, string innerTypeName)
    {
        return type.GetNestedTypes().First(x => x.Name.Contains(innerTypeName));
    }
}
