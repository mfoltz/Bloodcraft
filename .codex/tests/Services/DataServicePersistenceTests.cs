using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Bloodcraft.Services;
using Bloodcraft.Tests.Support;
using Xunit;
using PlayerDictionaries = Bloodcraft.Services.DataService.PlayerDictionaries;
using static Bloodcraft.Services.DataService.PlayerPersistence;

namespace Bloodcraft.Tests.Services;

public sealed class DataServicePersistenceTests : TestHost
{
    static readonly FieldInfo SuppressionDepthField = typeof(DataService).GetField("persistenceSuppressionDepth", BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("Unable to locate the persistence suppression depth field.");
    static readonly PropertyInfo IsSuppressedProperty = typeof(DataService).GetProperty("IsPersistenceSuppressed", BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("Unable to locate the persistence suppression property.");

    protected override void ResetState()
    {
        base.ResetState();
        ResetSuppressionDepth();
    }

    public override void Dispose()
    {
        try
        {
            ResetSuppressionDepth();
        }
        finally
        {
            base.Dispose();
        }
    }

    [Fact]
    public void SuppressPersistence_IsReferenceCounted()
    {
        ConfigDirectoryShim.EnsureInitialized();
        Assert.False(GetIsPersistenceSuppressed());
        Assert.Equal(0, GetSuppressionDepth());

        var outerScope = DataService.SuppressPersistence();
        try
        {
            Assert.True(GetIsPersistenceSuppressed());
            Assert.Equal(1, GetSuppressionDepth());

            var innerScope = DataService.SuppressPersistence();
            try
            {
                Assert.True(GetIsPersistenceSuppressed());
                Assert.Equal(2, GetSuppressionDepth());
            }
            finally
            {
                innerScope.Dispose();
            }

            Assert.True(GetIsPersistenceSuppressed());
            Assert.Equal(1, GetSuppressionDepth());
        }
        finally
        {
            outerScope.Dispose();
            Assert.False(GetIsPersistenceSuppressed());
            Assert.Equal(0, GetSuppressionDepth());

            outerScope.Dispose();
            Assert.Equal(0, GetSuppressionDepth());
        }
    }

    [Fact]
    public void SavePlayerExperience_DoesNotPersistWhileSuppressed()
    {
        ConfigDirectoryShim.EnsureInitialized();

        using var dataScope = CapturePlayerData();
        const ulong playerId = 12345;
        PlayerDictionaries._playerExperience[playerId] = new KeyValuePair<int, float>(15, 23.5f);

        using var directoryScope = new TestDirectoryScope("Experience");
        var experiencePath = directoryScope.Paths["Experience"];
        File.WriteAllText(experiencePath, "SENTINEL");
        var originalTimestamp = File.GetLastWriteTimeUtc(experiencePath);

        using var suppressionScope = DataService.SuppressPersistence();
        Assert.True(GetIsPersistenceSuppressed());
        SavePlayerExperience();

        Assert.Equal("SENTINEL", File.ReadAllText(experiencePath));
        Assert.Equal(originalTimestamp, File.GetLastWriteTimeUtc(experiencePath));
    }

    [Fact]
    public void JsonFilePaths_AreRegisteredInFileMap()
    {
        ConfigDirectoryShim.EnsureInitialized();
        var persistenceType = typeof(DataService).GetNestedType("PlayerPersistence", BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Unable to locate the player persistence type.");
        var jsonPathType = persistenceType.GetNestedType("JsonFilePaths", BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Unable to locate the JsonFilePaths container.");

        var expectedPaths = jsonPathType
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(field => (string)field.GetValue(null)!)
            .ToHashSet(StringComparer.Ordinal);

        var filePaths = GetFilePathMap();
        Assert.NotEmpty(filePaths);

        foreach (var entry in filePaths)
        {
            Assert.Contains(entry.Value, expectedPaths);
        }
    }

    static Dictionary<string, string> GetFilePathMap()
    {
        ConfigDirectoryShim.EnsureInitialized();

        var persistenceType = typeof(DataService).GetNestedType("PlayerPersistence", BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Unable to locate the player persistence type.");
        var field = persistenceType.GetField("_filePaths", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("Unable to locate the persistence file map.");

        return (Dictionary<string, string>)field.GetValue(null)!;
    }

    static bool GetIsPersistenceSuppressed() => (bool)IsSuppressedProperty.GetValue(null)!;

    static int GetSuppressionDepth() => (int)SuppressionDepthField.GetValue(null)!;

    static void ResetSuppressionDepth() => SuppressionDepthField.SetValue(null, 0);
}
