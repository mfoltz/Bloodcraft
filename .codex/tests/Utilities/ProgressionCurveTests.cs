using System;
using System.Collections.Generic;
using Bloodcraft.Utilities;
using Xunit;

namespace Bloodcraft.Tests.Utilities;

public sealed class ProgressionCurveTests
{
    public static IEnumerable<object[]> LevelXpFixtures()
    {
        yield return new object[] { 0, 0 };
        yield return new object[] { 1, 100 };
        yield return new object[] { 5, 2500 };
        yield return new object[] { 10, 10000 };
        yield return new object[] { 25, 62500 };
        yield return new object[] { 50, 250000 };
    }

    [Theory]
    [MemberData(nameof(LevelXpFixtures))]
    public void ConvertLevelToXp_MatchesFixture(int level, int expectedXp)
    {
        Assert.Equal(expectedXp, Progression.ConvertLevelToXp(level));
    }

    [Theory]
    [MemberData(nameof(LevelXpFixtures))]
    public void ConvertXpToLevel_MatchesFixture(int expectedLevel, int xp)
    {
        Assert.Equal(expectedLevel, Progression.ConvertXpToLevel(xp));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public void ConvertLevelAndXp_RoundTripBoundaries(int level)
    {
        int threshold = Progression.ConvertLevelToXp(level);
        int justBelow = Math.Max(0, threshold - 1);

        Assert.Equal(level, Progression.ConvertXpToLevel(threshold));
        Assert.Equal(Math.Max(0, level - 1), Progression.ConvertXpToLevel(justBelow));
    }
}
