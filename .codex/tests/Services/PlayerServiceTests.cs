using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Serialization;
using Bloodcraft.Services;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Tests.Services;

public sealed class PlayerServiceTests : TestHost
{
    protected override void ResetState()
    {
        base.ResetState();
        PlayerServiceTestState.ResetCaches();
        EntityQueryStub.Reset();
        EntityRegistry.Reset();
        UserStubFactory.Reset();
        EclipseServiceSpy.Reset();
    }

    [Fact]
    public void BuildPlayerInfoCache_UsesStubbedQueryResults()
    {
        using var config = WithConfigOverrides(("LevelingSystem", false), ("ExoPrestiging", false));

        Entity firstUserEntity = EntityRegistry.CreateEntity(1);
        Entity secondUserEntity = EntityRegistry.CreateEntity(2);
        Entity firstCharacterEntity = EntityRegistry.CreateEntity(101);
        Entity secondCharacterEntity = EntityRegistry.CreateEntity(102);

        User firstUser = UserStubFactory.CreateUser(1111, "Lazlo", true, firstCharacterEntity);
        User secondUser = UserStubFactory.CreateUser(2222, "Nandor", false, secondCharacterEntity);

        using var registry = EntityRegistry.Create(
            (firstUserEntity, firstUser, true),
            (secondUserEntity, secondUser, true));

        QueryDesc queryDescriptor = EntityQueryStub.CreateDescriptor(firstUserEntity, secondUserEntity);
        PlayerServiceTestState.SetUserQueryDescriptor(queryDescriptor);

        PlayerServiceTestState.InvokeBuildPlayerInfoCache();

        IReadOnlyDictionary<ulong, PlayerService.PlayerInfo> allPlayers = PlayerServiceTestState.GetPlayerCache();
        IReadOnlyDictionary<ulong, PlayerService.PlayerInfo> onlinePlayers = PlayerServiceTestState.GetOnlinePlayerCache();

        Assert.Equal(2, allPlayers.Count);
        Assert.True(allPlayers.TryGetValue(1111, out PlayerService.PlayerInfo onlineInfo));
        Assert.Equal(firstUserEntity, onlineInfo.UserEntity);
        Assert.Equal(firstCharacterEntity, onlineInfo.CharEntity);
        Assert.Equal("Lazlo", onlineInfo.User.CharacterName.Value);

        Assert.True(allPlayers.TryGetValue(2222, out PlayerService.PlayerInfo offlineInfo));
        Assert.Equal(secondUserEntity, offlineInfo.UserEntity);
        Assert.Equal(secondCharacterEntity, offlineInfo.CharEntity);
        Assert.Equal("Nandor", offlineInfo.User.CharacterName.Value);

        Assert.Single(onlinePlayers);
        Assert.True(onlinePlayers.ContainsKey(1111));
        Assert.False(onlinePlayers.ContainsKey(2222));
    }

    [Fact]
    public void HandleConnectionAndDisconnection_UpdateCachesAndInvokeCleanup()
    {
        using var config = WithConfigOverrides(("LevelingSystem", false), ("ExoPrestiging", false));

        Entity userEntity = EntityRegistry.CreateEntity(9);
        Entity characterEntity = EntityRegistry.CreateEntity(909);
        User user = UserStubFactory.CreateUser(8888, "Guillermo", true, characterEntity);

        var info = new PlayerService.PlayerInfo(userEntity, characterEntity, user);

        using var registry = EntityRegistry.Create((userEntity, user, true));
        using var cleanupSpy = EclipseServiceSpy.Create();

        PlayerService.HandleConnection(8888, info);

        IReadOnlyDictionary<ulong, PlayerService.PlayerInfo> onlinePlayers = PlayerServiceTestState.GetOnlinePlayerCache();
        IReadOnlyDictionary<ulong, PlayerService.PlayerInfo> allPlayers = PlayerServiceTestState.GetPlayerCache();

        Assert.True(onlinePlayers.ContainsKey(8888));
        Assert.True(allPlayers.ContainsKey(8888));

        PlayerService.HandleDisconnection(8888);

        onlinePlayers = PlayerServiceTestState.GetOnlinePlayerCache();
        allPlayers = PlayerServiceTestState.GetPlayerCache();

        Assert.False(onlinePlayers.ContainsKey(8888));
        Assert.True(allPlayers.ContainsKey(8888));

        Assert.True(cleanupSpy.TryRemovePreRegistrationCalled);
        Assert.True(cleanupSpy.TryUnregisterUserCalled);
    }

    private sealed class PlayerServiceTestState
    {
        static readonly Type PlayerServiceType = typeof(PlayerService);
        static readonly FieldInfo UserQueryField = PlayerServiceType.GetField("_userQueryDesc", BindingFlags.Static | BindingFlags.NonPublic)!;
        static readonly FieldInfo PlayerCacheField = PlayerServiceType.GetField("_steamIdPlayerInfoCache", BindingFlags.Static | BindingFlags.NonPublic)!;
        static readonly FieldInfo OnlineCacheField = PlayerServiceType.GetField("_steamIdOnlinePlayerInfoCache", BindingFlags.Static | BindingFlags.NonPublic)!;
        static readonly MethodInfo BuildCacheMethod = PlayerServiceType.GetMethod("BuildPlayerInfoCache", BindingFlags.Static | BindingFlags.NonPublic)!;

        public static void ResetCaches()
        {
            ((ConcurrentDictionary<ulong, PlayerService.PlayerInfo>)PlayerCacheField.GetValue(null)!).Clear();
            ((ConcurrentDictionary<ulong, PlayerService.PlayerInfo>)OnlineCacheField.GetValue(null)!).Clear();
        }

        public static void SetUserQueryDescriptor(QueryDesc descriptor)
        {
            UserQueryField.SetValue(null, descriptor);
        }

        public static void InvokeBuildPlayerInfoCache()
        {
            BuildCacheMethod.Invoke(null, Array.Empty<object>());
        }

        public static IReadOnlyDictionary<ulong, PlayerService.PlayerInfo> GetPlayerCache()
        {
            return (ConcurrentDictionary<ulong, PlayerService.PlayerInfo>)PlayerCacheField.GetValue(null)!;
        }

        public static IReadOnlyDictionary<ulong, PlayerService.PlayerInfo> GetOnlinePlayerCache()
        {
            return (ConcurrentDictionary<ulong, PlayerService.PlayerInfo>)OnlineCacheField.GetValue(null)!;
        }
    }

    private sealed class EntityRegistry : IDisposable
    {
        static readonly Harmony HarmonyInstance = new("Bloodcraft.Tests.PlayerService.EntityRegistry");
        static bool patched;
        static readonly List<EntityRegistry> Active = new();
        static readonly MethodInfo ExistsMethod = AccessTools.Method(typeof(VExtensions), nameof(VExtensions.Exists));
        static readonly MethodInfo GetUserMethod = AccessTools.Method(typeof(VExtensions), nameof(VExtensions.GetUser));

        readonly Dictionary<Entity, Entry> entries;

        private EntityRegistry(Dictionary<Entity, Entry> entries)
        {
            this.entries = entries;
            Active.Add(this);
            EnsurePatched();
        }

        public static EntityRegistry Create(params (Entity entity, User user, bool exists)[] definitions)
        {
            var comparer = new EntityComparer();
            var map = new Dictionary<Entity, Entry>(comparer);

            foreach (var definition in definitions)
            {
                map[definition.entity] = new Entry(definition.user, definition.exists);
            }

            return new EntityRegistry(map);
        }

        public static Entity CreateEntity(int identifier)
        {
            return new Entity { Index = identifier, Version = 1 };
        }

        public static void Reset()
        {
            if (!patched)
            {
                return;
            }

            HarmonyInstance.Unpatch(ExistsMethod, HarmonyPatchType.Prefix, HarmonyInstance.Id);
            HarmonyInstance.Unpatch(GetUserMethod, HarmonyPatchType.Prefix, HarmonyInstance.Id);
            Active.Clear();
            patched = false;
        }

        static void EnsurePatched()
        {
            if (patched)
            {
                return;
            }

            HarmonyInstance.Patch(ExistsMethod, prefix: new HarmonyMethod(typeof(EntityRegistry), nameof(ExistsPrefix)));
            HarmonyInstance.Patch(GetUserMethod, prefix: new HarmonyMethod(typeof(EntityRegistry), nameof(GetUserPrefix)));
            patched = true;
        }

        static bool ExistsPrefix(Entity entity, ref bool __result)
        {
            if (TryGetEntry(entity, out Entry entry))
            {
                __result = entry.Exists;
                return false;
            }

            return true;
        }

        static bool GetUserPrefix(Entity entity, ref User __result)
        {
            if (TryGetEntry(entity, out Entry entry))
            {
                __result = entry.User;
                return false;
            }

            return true;
        }

        static bool TryGetEntry(Entity entity, out Entry entry)
        {
            for (int i = Active.Count - 1; i >= 0; i--)
            {
                if (Active[i].entries.TryGetValue(entity, out entry))
                {
                    return true;
                }
            }

            entry = default;
            return false;
        }

        public void Dispose()
        {
            Active.Remove(this);

            if (!Active.Any())
            {
                Reset();
            }
        }

        readonly record struct Entry(User User, bool Exists);

        sealed class EntityComparer : IEqualityComparer<Entity>
        {
            public bool Equals(Entity x, Entity y)
            {
                return x.Index == y.Index && x.Version == y.Version;
            }

            public int GetHashCode(Entity obj)
            {
                return HashCode.Combine(obj.Index, obj.Version);
            }
        }
    }

    private sealed class EntityQueryStub
    {
        static readonly Harmony HarmonyInstance = new("Bloodcraft.Tests.PlayerService.EntityQueryStub");
        static readonly MethodInfo ToEntityArrayMethod = AccessTools.Method(typeof(EntityQuery), nameof(EntityQuery.ToEntityArray), new[] { typeof(Allocator) });
        static bool patched;
        static readonly List<Dictionary<EntityQuery, Entity[]>> Active = new();

        public static QueryDesc CreateDescriptor(params Entity[] entities)
        {
            EntityQuery query = default;
            var map = new Dictionary<EntityQuery, Entity[]>
            {
                [query] = entities
            };

            Active.Add(map);
            EnsurePatched();
            return new QueryDesc(query, Array.Empty<ComponentType>(), Array.Empty<int>());
        }

        public static void Reset()
        {
            Active.Clear();

            if (patched)
            {
                HarmonyInstance.Unpatch(ToEntityArrayMethod, HarmonyPatchType.Prefix, HarmonyInstance.Id);
                patched = false;
            }
        }

        static void EnsurePatched()
        {
            if (patched)
            {
                return;
            }

            HarmonyInstance.Patch(ToEntityArrayMethod, prefix: new HarmonyMethod(typeof(EntityQueryStub), nameof(Prefix)));
            patched = true;
        }

        static bool Prefix(EntityQuery __instance, Allocator allocator, ref NativeArray<Entity> __result)
        {
            for (int i = Active.Count - 1; i >= 0; i--)
            {
                if (Active[i].TryGetValue(__instance, out Entity[]? entities))
                {
                    var nativeArray = new NativeArray<Entity>(entities.Length, allocator, NativeArrayOptions.UninitializedMemory);

                    for (int j = 0; j < entities.Length; j++)
                    {
                        nativeArray[j] = entities[j];
                    }

                    __result = nativeArray;
                    return false;
                }
            }

            return true;
        }
    }

    private sealed class UserStubFactory
    {
        static readonly Type UserType = typeof(User);
        static readonly MemberAccessor PlatformIdAccessor = MemberAccessor.Create(UserType, "PlatformId");
        static readonly MemberAccessor IsConnectedAccessor = MemberAccessor.Create(UserType, "IsConnected");
        static readonly MemberAccessor CharacterNameAccessor = MemberAccessor.Create(UserType, "CharacterName");
        static readonly MemberAccessor LocalCharacterAccessor = MemberAccessor.Create(UserType, "LocalCharacter");

        static readonly Type CharacterNameType = CharacterNameAccessor.MemberType;
        static readonly MemberAccessor CharacterNameValueAccessor = MemberAccessor.Create(CharacterNameType, "Value");

        static readonly Harmony HarmonyInstance = new("Bloodcraft.Tests.PlayerService.UserLocalCharacter");
        static bool patched;
        static readonly Dictionary<object, Entity> LocalCharacterEntities = new();
        static readonly object Sync = new();

        public static User CreateUser(ulong platformId, string name, bool isConnected, Entity characterEntity)
        {
            var user = (User)FormatterServices.GetUninitializedObject(UserType);
            PlatformIdAccessor.SetValue(ref user, platformId);
            IsConnectedAccessor.SetValue(ref user, isConnected);

            object characterName = FormatterServices.GetUninitializedObject(CharacterNameType);
            CharacterNameValueAccessor.SetValue(characterName, name);
            CharacterNameAccessor.SetValue(ref user, characterName);

            object localCharacter = FormatterServices.GetUninitializedObject(LocalCharacterAccessor.MemberType);
            lock (Sync)
            {
                LocalCharacterEntities[localCharacter] = characterEntity;
            }

            LocalCharacterAccessor.SetValue(ref user, localCharacter);
            EnsurePatched(LocalCharacterAccessor.MemberType);
            return user;
        }

        static void EnsurePatched(Type localCharacterType)
        {
            if (patched)
            {
                return;
            }

            MethodInfo? getEntityOnServer = localCharacterType.GetMethod("GetEntityOnServer", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (getEntityOnServer is null)
            {
                throw new InvalidOperationException("Unable to locate GetEntityOnServer on LocalCharacter type.");
            }

            HarmonyInstance.Patch(getEntityOnServer, prefix: new HarmonyMethod(typeof(UserStubFactory), nameof(GetEntityOnServerPrefix)));
            patched = true;
        }

        static bool GetEntityOnServerPrefix(object __instance, ref Entity __result)
        {
            lock (Sync)
            {
                if (LocalCharacterEntities.TryGetValue(__instance, out Entity entity))
                {
                    __result = entity;
                    return false;
                }
            }

            return true;
        }

        public static void Reset()
        {
            lock (Sync)
            {
                LocalCharacterEntities.Clear();
            }

            if (patched)
            {
                HarmonyInstance.UnpatchAll(HarmonyInstance.Id);
                patched = false;
            }
        }
    }

    private sealed class EclipseServiceSpy : IDisposable
    {
        static readonly Harmony HarmonyInstance = new("Bloodcraft.Tests.PlayerService.EclipseService");
        static readonly MethodInfo TryRemovePreRegistrationMethod = AccessTools.Method(typeof(EclipseService), nameof(EclipseService.TryRemovePreRegistration));
        static readonly MethodInfo TryUnregisterUserMethod = AccessTools.Method(typeof(EclipseService), nameof(EclipseService.TryUnregisterUser));

        static bool patched;
        static EclipseServiceSpy? active;

        public bool TryRemovePreRegistrationCalled { get; private set; }
        public bool TryUnregisterUserCalled { get; private set; }

        private EclipseServiceSpy()
        {
            active = this;
            EnsurePatched();
        }

        public static EclipseServiceSpy Create()
        {
            return new EclipseServiceSpy();
        }

        static void EnsurePatched()
        {
            if (patched)
            {
                return;
            }

            HarmonyInstance.Patch(TryRemovePreRegistrationMethod, prefix: new HarmonyMethod(typeof(EclipseServiceSpy), nameof(TryRemovePrefix)));
            HarmonyInstance.Patch(TryUnregisterUserMethod, prefix: new HarmonyMethod(typeof(EclipseServiceSpy), nameof(TryUnregisterPrefix)));
            patched = true;
        }

        public static void Reset()
        {
            active = null;
            if (patched)
            {
                HarmonyInstance.Unpatch(TryRemovePreRegistrationMethod, HarmonyPatchType.Prefix, HarmonyInstance.Id);
                HarmonyInstance.Unpatch(TryUnregisterUserMethod, HarmonyPatchType.Prefix, HarmonyInstance.Id);
                patched = false;
            }
        }

        static bool TryRemovePrefix(ulong steamId)
        {
            if (active is not null)
            {
                active.TryRemovePreRegistrationCalled = true;
                return false;
            }

            return true;
        }

        static bool TryUnregisterPrefix(ulong steamId)
        {
            if (active is not null)
            {
                active.TryUnregisterUserCalled = true;
                return false;
            }

            return true;
        }

        public void Dispose()
        {
            active = null;
            Reset();
        }
    }

    private sealed class MemberAccessor
    {
        readonly FieldInfo? field;
        readonly PropertyInfo? property;

        MemberAccessor(FieldInfo? field, PropertyInfo? property)
        {
            this.field = field;
            this.property = property;
        }

        public Type MemberType => field?.FieldType ?? property!.PropertyType;

        public static MemberAccessor Create(Type type, string name)
        {
            FieldInfo? field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            PropertyInfo? property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (field is null && property is null)
            {
                throw new InvalidOperationException($"Unable to locate member '{name}' on type '{type}'.");
            }

            return new MemberAccessor(field, property);
        }

        public void SetValue(object instance, object value)
        {
            if (field is not null)
            {
                field.SetValue(instance, value);
            }
            else
            {
                property!.SetValue(instance, value);
            }
        }

        public void SetValue(ref User user, object value)
        {
            object boxed = user;

            if (field is not null)
            {
                field.SetValue(boxed, value);
            }
            else
            {
                property!.SetValue(boxed, value);
            }

            user = (User)boxed;
        }
    }
}
