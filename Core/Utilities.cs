using Bloodstone.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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