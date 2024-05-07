using Cobalt.Core;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.StunDebug;
using ProjectM.UI;
using RootMotion;
using Stunlock.Core.Authoring;
using StunShared.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Rendering;

namespace Cobalt.Hooks
{
    [HarmonyPatch]
    public class TestPatchesPleaseIgnore
    {
        [HarmonyPatch(typeof(ChatMessageSystem), nameof(InteractSystemHUD.OnUpdate))]
        [HarmonyPrefix]
        private static void InteractSystemHUDPrefix(InteractSystemHUD __instance)
        {
            Plugin.Log.LogInfo("InteractSystemHUDPrefix...");
            NativeArray<Entity> entities = __instance.__query_611024430_0.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (Entity entity in entities)
                {
                    entity.LogComponentTypes();
                }
            }
            finally
            {
                entities.Dispose();
            }
            entities = __instance.__query_611024430_1.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (Entity entity in entities)
                {
                    entity.LogComponentTypes();
                }
            }
            finally
            {
                entities.Dispose();
            }
            entities = __instance.__query_611024430_2.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (Entity entity in entities)
                {
                    entity.LogComponentTypes();
                }
            }
            finally
            {
                entities.Dispose();
            }
        }
        
    }
}
