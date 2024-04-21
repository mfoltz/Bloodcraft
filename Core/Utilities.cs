using Bloodstone.API;
using Unity.Collections;
using Unity.Entities;

namespace Cobalt.Core
{
    public static class Utilities
    {
        public static void LogComponentTypes(this Entity entity)
        {
            NativeArray<ComponentType>.Enumerator enumerator = VWorld.Server.EntityManager.GetComponentTypes(entity).GetEnumerator();
            while (enumerator.MoveNext())
            {
                ComponentType current = enumerator.Current;
                Plugin.Log.LogInfo($"{current}");
            }
            Plugin.Log.LogInfo("===");
        }
    }
}