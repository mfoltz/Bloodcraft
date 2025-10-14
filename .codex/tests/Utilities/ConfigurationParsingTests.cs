using System.Collections.Generic;
using Bloodcraft.Utilities;
using Xunit;

namespace Bloodcraft.Tests.Utilities;

public sealed class ConfigurationParsingTests
{
    [Fact]
    public void ParseIntegersFromString_ReturnsEmptyList_ForNullOrWhitespace()
    {
        Assert.Empty(Configuration.ParseIntegersFromString(null!));
        Assert.Empty(Configuration.ParseIntegersFromString(string.Empty));
        Assert.Empty(Configuration.ParseIntegersFromString("   \t"));
    }

    [Fact]
    public void ParseIntegersFromString_TrimsWhitespaceAndIgnoresEmptyEntries()
    {
        string input = " 1, 2 ,3,, 4 , ,5";

        List<int> result = Configuration.ParseIntegersFromString(input);

        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, result);
    }

    [Fact]
    public void ParseEnumsFromString_IsCaseInsensitiveAndSkipsInvalidTokens()
    {
        string input = "alpha, BETA, unknown, gamma,";

        List<TestEnum> result = Configuration.ParseEnumsFromString<TestEnum>(input);

        Assert.Equal(new[] { TestEnum.Alpha, TestEnum.Beta, TestEnum.Gamma }, result);
    }

    private enum TestEnum
    {
        Alpha,
        Beta,
        Gamma
    }
}
