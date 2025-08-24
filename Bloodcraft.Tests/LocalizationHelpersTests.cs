using Bloodcraft.Utilities;

namespace Bloodcraft.Tests;

public class LocalizationHelpersTests
{
    [Theory]
    [InlineData("Hello <b>world</b> {player}")]
    [InlineData("<color=red>${blood}</color> {another}")]
    [InlineData("Just plain text")]
    [InlineData("Look [here] for loot")]
    [InlineData("Progress is 50% complete")]
    [InlineData("<i>[target]</i> gains {amount}% ${bonus}")]
    public void ProtectUnprotect_ReturnsOriginal(string input)
    {
        Dictionary<string, string> map = new();
        string protectedText = LocalizationHelpers.ProtectTokens(input, map);
        string unprotectedText = LocalizationHelpers.UnprotectTokens(protectedText, map);
        Assert.Equal(input, unprotectedText);
    }
}