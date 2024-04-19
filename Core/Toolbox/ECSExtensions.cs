using Bloodstone.API;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using ProjectM;
using System.Runtime.InteropServices;
using Unity;
using Unity.Collections;
using Unity.Entities;

namespace VCreate.Core.Toolbox;

public static class ECSExtensions
{
    public static unsafe void Write<T>(this Entity entity, T componentData) where T : struct
    {
        ComponentType componentType = new ComponentType(Il2CppType.Of<T>());
        byte[] array = StructureToByteArray(componentData);
        int size = Marshal.SizeOf<T>();
        fixed (byte* data = array)
        {
            VWorld.Server.EntityManager.SetComponentDataRaw(entity, componentType.TypeIndex, data, size);
        }
    }

    public static byte[] StructureToByteArray<T>(T structure) where T : struct
    {
        int num = Marshal.SizeOf(structure);
        byte[] array = new byte[num];
        IntPtr intPtr = Marshal.AllocHGlobal(num);
        Marshal.StructureToPtr(structure, intPtr, fDeleteOld: true);
        Marshal.Copy(intPtr, array, 0, num);
        Marshal.FreeHGlobal(intPtr);
        return array;
    }

    public static unsafe T Read<T>(this Entity entity) where T : struct
    {
        ComponentType componentType = new ComponentType(Il2CppType.Of<T>());
        void* componentDataRawRO = VWorld.Server.EntityManager.GetComponentDataRawRO(entity, componentType.TypeIndex);
        return Marshal.PtrToStructure<T>(new IntPtr(componentDataRawRO));
    }

    public static DynamicBuffer<T> ReadBuffer<T>(this Entity entity) where T : struct
    {
        return VWorld.Server.EntityManager.GetBuffer<T>(entity);
    }

    public static void Add<T>(this Entity entity)
    {
        ComponentType componentType = new ComponentType(Il2CppType.Of<T>());
        VWorld.Server.EntityManager.AddComponent(entity, componentType);
    }

    public static void Remove<T>(this Entity entity)
    {
        ComponentType componentType = new ComponentType(Il2CppType.Of<T>());
        VWorld.Server.EntityManager.RemoveComponent(entity, componentType);
    }

    public static bool Has<T>(this Entity entity)
    {
        ComponentType type = new ComponentType(Il2CppType.Of<T>());
        return VWorld.Server.EntityManager.HasComponent(entity, type);
    }

    public static void LogComponentTypes(this Entity entity)
    {
        NativeArray<ComponentType>.Enumerator enumerator = VWorld.Server.EntityManager.GetComponentTypes(entity).GetEnumerator();
        while (enumerator.MoveNext())
        {
            ComponentType current = enumerator.Current;
            Debug.Log($"{current}");

        }

        Debug.Log("===");

    }
    public static List<ComponentType> GetComponentTypes(this Entity entity)
    {
        List<ComponentType> componentTypes = new List<ComponentType>();
        NativeArray<ComponentType> types = VWorld.Server.EntityManager.GetComponentTypes(entity);

        foreach (var current in types)
        {
            Debug.Log($"{current}");
            componentTypes.Add(current);
        }

        Debug.Log("===");
        types.Dispose(); // Don't forget to dispose of the NativeArray when you're done with it
        return componentTypes;
    }


    public static void LogComponentTypes(this EntityQuery entityQuery)
    {
        Il2CppStructArray<ComponentType> queryTypes = entityQuery.GetQueryTypes();
        foreach (ComponentType item in queryTypes)
        {
            Debug.Log($"Query Component Type: {item}");
        }

        Debug.Log("===");
    }

    public static string LookupName(this PrefabGUID prefabGuid)
    {
        PrefabCollectionSystem existingSystem = VWorld.Server.GetExistingSystem<PrefabCollectionSystem>();
        object obj;
        if (!existingSystem.PrefabGuidToNameDictionary.ContainsKey(prefabGuid))
        {
            obj = "GUID Not Found";
        }
        else
        {
            string text = existingSystem.PrefabGuidToNameDictionary[prefabGuid];
            PrefabGUID prefabGUID = prefabGuid;
            obj = text + " " + prefabGUID.ToString();
        }

        return obj.ToString();
    }
}

#if false // Decompilation log
'342' items in cache
------------------
Resolve: 'System.Runtime, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Users\mitch\.nuget\packages\microsoft.netcore.app.ref\6.0.27\ref\net6.0\System.Runtime.dll'
------------------
Resolve: 'System.Collections, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Users\mitch\.nuget\packages\microsoft.netcore.app.ref\6.0.27\ref\net6.0\System.Collections.dll'
------------------
Resolve: 'Unity.Entities, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'Unity.Entities, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\mitch\.nuget\packages\vrising.unhollowed.client\0.6.5.57575090\lib\net6.0\Unity.Entities.dll'
------------------
Resolve: 'UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\mitch\.nuget\packages\vrising.unhollowed.client\0.6.5.57575090\lib\net6.0\UnityEngine.CoreModule.dll'
------------------
Resolve: 'ProjectM, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'ProjectM, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\mitch\.nuget\packages\vrising.unhollowed.client\0.6.5.57575090\lib\net6.0\ProjectM.dll'
------------------
Resolve: '0Harmony, Version=2.10.1.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: '0Harmony, Version=2.10.1.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\mitch\.nuget\packages\harmonyx\2.10.1\lib\netstandard2.0\0Harmony.dll'
------------------
Resolve: 'BepInEx.Unity.IL2CPP, Version=6.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'BepInEx.Unity.IL2CPP, Version=6.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\mitch\.nuget\packages\bepinex.unity.il2cpp\6.0.0-be.668\lib\net6.0\BepInEx.Unity.IL2CPP.dll'
------------------
Resolve: 'Il2CppInterop.Runtime, Version=1.4.5.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'Il2CppInterop.Runtime, Version=1.4.5.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\mitch\.nuget\packages\vrising.unhollowed.client\0.6.5.57575090\lib\net6.0\Il2CppInterop.Runtime.dll'
------------------
Resolve: 'Stunlock.Core, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'Stunlock.Core, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\mitch\.nuget\packages\vrising.unhollowed.client\0.6.5.57575090\lib\net6.0\Stunlock.Core.dll'
------------------
Resolve: 'ProjectM.Shared, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'ProjectM.Shared, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\mitch\.nuget\packages\vrising.unhollowed.client\0.6.5.57575090\lib\net6.0\ProjectM.Shared.dll'
------------------
Resolve: 'ProjectM.Gameplay.Systems, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'ProjectM.Gameplay.Systems, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\mitch\.nuget\packages\vrising.unhollowed.client\0.6.5.57575090\lib\net6.0\ProjectM.Gameplay.Systems.dll'
------------------
Resolve: 'ProjectM.Misc.Systems, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'ProjectM.Misc.Systems, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\mitch\.nuget\packages\vrising.unhollowed.client\0.6.5.57575090\lib\net6.0\ProjectM.Misc.Systems.dll'
------------------
Resolve: 'Unity.Collections, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'Unity.Collections, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\mitch\.nuget\packages\vrising.unhollowed.client\0.6.5.57575090\lib\net6.0\Unity.Collections.dll'
------------------
Resolve: 'ProjectM.Roofs, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'ProjectM.Roofs, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\mitch\.nuget\packages\vrising.unhollowed.client\0.6.5.57575090\lib\net6.0\ProjectM.Roofs.dll'
------------------
Resolve: 'Unity.Mathematics, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'Unity.Mathematics, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\mitch\.nuget\packages\vrising.unhollowed.client\0.6.5.57575090\lib\net6.0\Unity.Mathematics.dll'
------------------
Resolve: 'Il2Cppmscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'Il2Cppmscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\mitch\.nuget\packages\vrising.unhollowed.client\0.6.5.57575090\lib\net6.0\Il2Cppmscorlib.dll'
------------------
Resolve: 'Unity.Transforms, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'Unity.Transforms, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\mitch\.nuget\packages\vrising.unhollowed.client\0.6.5.57575090\lib\net6.0\Unity.Transforms.dll'
------------------
Resolve: 'BepInEx.Core, Version=6.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'BepInEx.Core, Version=6.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\mitch\.nuget\packages\bepinex.core\6.0.0-be.668\lib\netstandard2.0\BepInEx.Core.dll'
------------------
Resolve: 'Bloodstone, Version=0.1.6.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'Bloodstone, Version=0.1.6.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\mitch\.nuget\packages\vrising.bloodstone\0.1.6\lib\net6.0\Bloodstone.dll'
------------------
Resolve: 'com.stunlock.network, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'com.stunlock.network, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\mitch\.nuget\packages\vrising.unhollowed.client\0.6.5.57575090\lib\net6.0\com.stunlock.network.dll'
------------------
Resolve: 'ProjectM.Terrain, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'ProjectM.Terrain, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\mitch\.nuget\packages\vrising.unhollowed.client\0.6.5.57575090\lib\net6.0\ProjectM.Terrain.dll'
------------------
Resolve: 'ProjectM.Gameplay.Scripting, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'ProjectM.Gameplay.Scripting, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\mitch\.nuget\packages\vrising.unhollowed.client\0.6.5.57575090\lib\net6.0\ProjectM.Gameplay.Scripting.dll'
------------------
Resolve: 'System.Text.Json, Version=6.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Found single assembly: 'System.Text.Json, Version=6.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Load from: 'C:\Users\mitch\.nuget\packages\microsoft.netcore.app.ref\6.0.27\ref\net6.0\System.Text.Json.dll'
------------------
Resolve: 'System.ObjectModel, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ObjectModel, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Users\mitch\.nuget\packages\microsoft.netcore.app.ref\6.0.27\ref\net6.0\System.ObjectModel.dll'
------------------
Resolve: 'VampireCommandFramework, Version=0.8.2.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'VampireCommandFramework, Version=0.8.2.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\mitch\.nuget\packages\vrising.vampirecommandframework\0.8.2\lib\net6.0\VampireCommandFramework.dll'
------------------
Resolve: 'System.Linq, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Linq, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Users\mitch\.nuget\packages\microsoft.netcore.app.ref\6.0.27\ref\net6.0\System.Linq.dll'
------------------
Resolve: 'System.Runtime.InteropServices, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime.InteropServices, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Users\mitch\.nuget\packages\microsoft.netcore.app.ref\6.0.27\ref\net6.0\System.Runtime.InteropServices.dll'
------------------
Resolve: 'ProjectM.CodeGeneration, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'ProjectM.CodeGeneration, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\mitch\.nuget\packages\vrising.unhollowed.client\0.6.5.57575090\lib\net6.0\ProjectM.CodeGeneration.dll'
------------------
Resolve: 'System.Text.RegularExpressions, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Text.RegularExpressions, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Users\mitch\.nuget\packages\microsoft.netcore.app.ref\6.0.27\ref\net6.0\System.Text.RegularExpressions.dll'
------------------
Resolve: 'System.Runtime.CompilerServices.Unsafe, Version=6.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'System.Runtime.CompilerServices.Unsafe, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Users\mitch\.nuget\packages\microsoft.netcore.app.ref\6.0.27\ref\net6.0\System.Runtime.CompilerServices.Unsafe.dll'
#endif