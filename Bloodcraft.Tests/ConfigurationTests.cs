using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM;
using Stunlock.Core;
using Xunit;

namespace Bloodcraft.Tests;

public class ConfigurationTests
{
    [Fact]
    public void ParsePrefabGuidsFromString_ParsesNumericGuid()
    {
        var result = Configuration.ParsePrefabGuidsFromString("123456");
        Assert.Single(result);
        Assert.Equal(new PrefabGUID(123456), result[0]);
    }

    [Fact]
    public void ParsePrefabGuidsFromString_ParsesNameCaseInsensitive()
    {
        PrefabGUID guid = new(654321);
        ((Dictionary<PrefabGUID, string>)LocalizationService.PrefabGuidNames)[guid] = "CHAR_Test_Prefab";

        var result = Configuration.ParsePrefabGuidsFromString("char_test_prefab");
        Assert.Single(result);
        Assert.Equal(guid, result[0]);
    }
}
