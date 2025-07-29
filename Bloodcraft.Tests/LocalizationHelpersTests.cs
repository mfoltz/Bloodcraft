using Bloodcraft.Utilities;

namespace Bloodcraft.Tests;

public class LocalizationHelpersTests
{
    [Theory]
    [InlineData("Hello <b>world</b> {player}")]
    [InlineData("<color=red>${blood}</color> {another}")]
    [InlineData("Just plain text")]
    public void ProtectUnprotect_ReturnsOriginal(string input)
    {
        Dictionary<string, string> map = new();
        string protectedText = LocalizationHelpers.ProtectTokens(input, map);
        string unprotectedText = LocalizationHelpers.UnprotectTokens(protectedText, map);
        Assert.Equal(input, unprotectedText);
    }
}