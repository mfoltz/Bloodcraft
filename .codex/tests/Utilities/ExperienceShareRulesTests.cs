using Bloodcraft.Utilities;
using Xunit;

namespace Bloodcraft.Tests.Utilities;

public sealed class ExperienceShareRulesTests
{
    [Fact]
    public void ShouldShareExperience_ReturnsFalse_WhenSharingDisabled()
    {
        bool result = Progression.ShouldShareExperience(
            experienceSharingEnabled: false,
            isPvE: true,
            targetHasPrestiged: true,
            levelDifference: 0,
            shareLevelRange: 0,
            areAllied: true,
            isIgnored: false);

        Assert.False(result);
    }

    [Fact]
    public void ShouldShareExperience_AllowsPrestigedPlayerInPvE()
    {
        bool result = Progression.ShouldShareExperience(
            experienceSharingEnabled: true,
            isPvE: true,
            targetHasPrestiged: true,
            levelDifference: 999,
            shareLevelRange: 5,
            areAllied: false,
            isIgnored: false);

        Assert.True(result);
    }

    [Fact]
    public void ShouldShareExperience_AllowsPlayersWithinLevelRangeInPvE()
    {
        bool withinRange = Progression.ShouldShareExperience(
            experienceSharingEnabled: true,
            isPvE: true,
            targetHasPrestiged: false,
            levelDifference: 4,
            shareLevelRange: 5,
            areAllied: false,
            isIgnored: false);

        bool outsideRange = Progression.ShouldShareExperience(
            experienceSharingEnabled: true,
            isPvE: true,
            targetHasPrestiged: false,
            levelDifference: 6,
            shareLevelRange: 5,
            areAllied: false,
            isIgnored: false);

        Assert.True(withinRange);
        Assert.False(outsideRange);
    }

    [Fact]
    public void ShouldShareExperience_RequiresAllianceInPvP()
    {
        bool allied = Progression.ShouldShareExperience(
            experienceSharingEnabled: true,
            isPvE: false,
            targetHasPrestiged: false,
            levelDifference: 0,
            shareLevelRange: 5,
            areAllied: true,
            isIgnored: false);

        bool notAllied = Progression.ShouldShareExperience(
            experienceSharingEnabled: true,
            isPvE: false,
            targetHasPrestiged: false,
            levelDifference: 0,
            shareLevelRange: 5,
            areAllied: false,
            isIgnored: false);

        Assert.True(allied);
        Assert.False(notAllied);
    }

    [Fact]
    public void ShouldShareExperience_ReturnsFalse_WhenPlayerIsIgnored()
    {
        bool result = Progression.ShouldShareExperience(
            experienceSharingEnabled: true,
            isPvE: true,
            targetHasPrestiged: true,
            levelDifference: 0,
            shareLevelRange: 0,
            areAllied: true,
            isIgnored: true);

        Assert.False(result);
    }
}
