using HarmonyLib;
using ProjectM;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

/*
[HarmonyPatch]
internal static class LoadPersistenceSystemPatch
{
    [HarmonyPatch(typeof(LoadPersistenceSystemV2), nameof(LoadPersistenceSystemV2.DeserializeSystemData))]
    [HarmonyPostfix]
    unsafe static void PreventDuplicateKeys(ref NativeParallelHashMap<Entity, Entity> fromOldToNewEntity) // This is likely the 'PrefabRemapping' field
    {
        NativeArray<Entity> keyArray = default;
        NativeList<Entity> seenKeys = default;
        NativeList<Entity> duplicates = default;

        try
        {
            // Get all unique keys into a NativeArray using GetUniqueKeyArray
            keyArray = fromOldToNewEntity.GetKeyArray(Allocator.TempJob);
            int length = keyArray.Length;

            // Store detected duplicates
            seenKeys = new NativeList<Entity>(length, Allocator.TempJob);
            duplicates = new NativeList<Entity>(length, Allocator.TempJob);

            // Iterate over the unique keys
            for (int i = 0; i < keyArray.Length; i++)
            {
                Entity key = keyArray[i];

                if (seenKeys.Contains(key))
                {
                    duplicates.AddNoResize(key); // Add duplicate key
                    Plugin.LogInstance.LogWarning($"Duplicate key detected: {key}");
                }
                else
                {
                    seenKeys.AddNoResize(key); // Add unique key
                }
            }

            // Handle duplicates (log or remove them)
            for (int i = 0; i < duplicates.Length; i++)
            {
                Entity duplicateKey = duplicates[i];
                fromOldToNewEntity.Remove(duplicateKey);
                Plugin.LogInstance.LogWarning($"Removed duplicate key: {duplicateKey}");
            }

            // Dispose of temporary containers
            keyArray.Dispose();
            seenKeys.Dispose();
            duplicates.Dispose();
        }
        catch (Exception ex)
        {
            Plugin.LogInstance.LogError($"Error while checking for duplicates: {ex.Message}\n{ex.StackTrace}");
        }
        finally
        {
            // Dispose of all temporary containers
            if (keyArray.IsCreated) keyArray.Dispose();
            if (seenKeys.IsCreated) seenKeys.Dispose();
            if (duplicates.IsCreated) duplicates.Dispose();
        }
    }
}
*/