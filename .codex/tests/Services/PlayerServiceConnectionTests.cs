using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Bloodcraft.Services;
using Bloodcraft.Tests;

namespace Bloodcraft.Tests.Services;

/// <summary>
/// Verifies the connection and disconnection helpers exposed by <see cref="PlayerService"/>.
/// </summary>
public sealed class PlayerServiceConnectionTests : TestHost
{
    const ulong SteamId = 123_456_789_012_345_678ul;
    const string LoadAssemblyEnvironmentVariable = "BLOODCRAFT_LOAD_GAME_ASSEMBLY";
    const string GameAssemblyRelativePath = "Support/libGameAssembly.so";

    /// <summary>
    /// Optionally loads the native GameAssembly payload when the caller explicitly opts in via
    /// <see cref="LoadAssemblyEnvironmentVariable"/>. The tests do not require the payload, so the
    /// default behavior is to skip loading the native binary.
    /// </summary>
    [ModuleInitializer]
    internal static void Initialize()
    {
        if (!ShouldLoadGameAssembly())
        {
            return;
        }

        string libraryPath = Path.Combine(AppContext.BaseDirectory, GameAssemblyRelativePath);

        if (!File.Exists(libraryPath))
        {
            throw new FileNotFoundException($"The GameAssembly payload was not found at '{libraryPath}'.", libraryPath);
        }

        _ = NativeLibrary.Load(libraryPath);
    }

    [Fact]
    public void HandleConnection_CachesPlayerInfo()
    {
        using var scope = new ConnectionStateScope();

        var playerInfo = new PlayerService.PlayerInfo();

        PlayerService.HandleConnection(SteamId, playerInfo);

        Assert.True(PlayerService.SteamIdPlayerInfoCache.ContainsKey(SteamId));
        Assert.True(PlayerService.SteamIdOnlinePlayerInfoCache.ContainsKey(SteamId));
    }

    [Fact]
    public void HandleDisconnection_RemovesOnlineEntryAndClearsEclipseState()
    {
        using var scope = new ConnectionStateScope();

        var playerInfo = new PlayerService.PlayerInfo();
        PlayerService.HandleConnection(SteamId, playerInfo);

        ConnectionStateScope.RegisteredUsers.TryAdd(SteamId, "1.3.0");
        EclipseService.HandlePreRegistration(SteamId);

        PlayerService.HandleDisconnection(SteamId);

        Assert.True(PlayerService.SteamIdPlayerInfoCache.ContainsKey(SteamId));
        Assert.False(PlayerService.SteamIdOnlinePlayerInfoCache.ContainsKey(SteamId));
        Assert.DoesNotContain(SteamId, EclipseService.PendingRegistration.Keys);
        Assert.DoesNotContain(SteamId, ConnectionStateScope.RegisteredUsers.Keys);
    }

    static bool ShouldLoadGameAssembly()
    {
        string? value = Environment.GetEnvironmentVariable(LoadAssemblyEnvironmentVariable);
        return string.Equals(value, "1", StringComparison.Ordinal) || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    sealed class ConnectionStateScope : IDisposable
    {
        internal static ConcurrentDictionary<ulong, string> RegisteredUsers => CacheAccessor.RegisteredUsers;

        readonly ConcurrentDictionary<ulong, PlayerService.PlayerInfo> playerCache;
        readonly ConcurrentDictionary<ulong, PlayerService.PlayerInfo> onlineCache;
        readonly ConcurrentDictionary<ulong, string> pendingRegistration;
        readonly ConcurrentDictionary<ulong, string> registeredUsers;

        readonly KeyValuePair<ulong, PlayerService.PlayerInfo>[] playerSnapshot;
        readonly KeyValuePair<ulong, PlayerService.PlayerInfo>[] onlineSnapshot;
        readonly KeyValuePair<ulong, string>[] pendingSnapshot;
        readonly KeyValuePair<ulong, string>[] registeredSnapshot;

        public ConnectionStateScope()
        {
            playerCache = CacheAccessor.PlayerCache;
            onlineCache = CacheAccessor.OnlineCache;
            pendingRegistration = CacheAccessor.PendingRegistration;
            registeredUsers = CacheAccessor.RegisteredUsers;

            playerSnapshot = playerCache.ToArray();
            onlineSnapshot = onlineCache.ToArray();
            pendingSnapshot = pendingRegistration.ToArray();
            registeredSnapshot = registeredUsers.ToArray();

            ClearCaches();
        }

        public void Dispose()
        {
            ClearCaches();
            RestoreSnapshot(playerCache, playerSnapshot);
            RestoreSnapshot(onlineCache, onlineSnapshot);
            RestoreSnapshot(pendingRegistration, pendingSnapshot);
            RestoreSnapshot(registeredUsers, registeredSnapshot);
        }

        void ClearCaches()
        {
            playerCache.Clear();
            onlineCache.Clear();
            pendingRegistration.Clear();
            registeredUsers.Clear();
        }

        static void RestoreSnapshot<T>(ConcurrentDictionary<ulong, T> cache, IReadOnlyList<KeyValuePair<ulong, T>> snapshot)
        {
            foreach (var entry in snapshot)
            {
                cache[entry.Key] = entry.Value;
            }
        }

        static class CacheAccessor
        {
            static readonly ConcurrentDictionary<ulong, PlayerService.PlayerInfo> playerCache = GetPlayerCache("_steamIdPlayerInfoCache");
            static readonly ConcurrentDictionary<ulong, PlayerService.PlayerInfo> onlineCache = GetPlayerCache("_steamIdOnlinePlayerInfoCache");
            static readonly ConcurrentDictionary<ulong, string> pendingRegistration = GetEclipseCache("_pendingRegistration");
            static readonly ConcurrentDictionary<ulong, string> registeredUsers = GetEclipseCache("_registeredUsersAndClientVersions");

            public static ConcurrentDictionary<ulong, PlayerService.PlayerInfo> PlayerCache => playerCache;
            public static ConcurrentDictionary<ulong, PlayerService.PlayerInfo> OnlineCache => onlineCache;
            public static ConcurrentDictionary<ulong, string> PendingRegistration => pendingRegistration;
            public static ConcurrentDictionary<ulong, string> RegisteredUsers => registeredUsers;

            static ConcurrentDictionary<ulong, PlayerService.PlayerInfo> GetPlayerCache(string fieldName)
            {
                return GetRequiredField(typeof(PlayerService), fieldName) switch
                {
                    FieldInfo field when field.GetValue(null) is ConcurrentDictionary<ulong, PlayerService.PlayerInfo> cache => cache,
                    _ => throw new InvalidOperationException($"The player cache '{fieldName}' could not be resolved.")
                };
            }

            static ConcurrentDictionary<ulong, string> GetEclipseCache(string fieldName)
            {
                return GetRequiredField(typeof(EclipseService), fieldName) switch
                {
                    FieldInfo field when field.GetValue(null) is ConcurrentDictionary<ulong, string> cache => cache,
                    _ => throw new InvalidOperationException($"The eclipse cache '{fieldName}' could not be resolved.")
                };
            }

            static FieldInfo GetRequiredField(Type type, string fieldName)
            {
                return type.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException($"The field '{fieldName}' could not be located on '{type.FullName}'.");
            }
        }
    }
}
