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
        [HarmonyPatch(typeof(ClaimedAchievementsClientSystem), nameof(ClaimedAchievementsClientSystem.OnUpdate))]
        [HarmonyPrefix]
        private static void AchievementPrefix(ClaimedAchievementsClientSystem __instance)
        {
            Plugin.Log.LogInfo("AchievementPrefix...");
            NativeArray<Entity> entities = __instance.__query_2001856168_0.ToEntityArray(Allocator.Temp);
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
            entities = __instance.__query_2001856168_1.ToEntityArray(Allocator.Temp);
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
        
        [HarmonyPatch(typeof(CommonClientDataSystem), nameof(CommonClientDataSystem.OnUpdate))]
        [HarmonyPrefix]
        private static void CommonClientDataSystemPrefix(CommonClientDataSystem __instance)
        {
            Plugin.Log.LogInfo("AchievementPrefix...");
            NativeArray<Entity> entities = __instance.__query_1840110765_0.ToEntityArray(Allocator.Temp);
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
            entities = __instance.__query_1840110765_1.ToEntityArray(Allocator.Temp);
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
            entities = __instance.__query_1840110765_2.ToEntityArray(Allocator.Temp);
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
            entities = __instance.__query_1840110765_3.ToEntityArray(Allocator.Temp);
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
            entities = __instance.__query_1840110765_4.ToEntityArray(Allocator.Temp);
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
