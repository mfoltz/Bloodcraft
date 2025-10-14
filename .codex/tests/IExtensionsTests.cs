namespace Bloodcraft.Tests;

public sealed class IExtensionsTests : TestHost
{
    [Fact]
    public void Reverse_ShouldSwapKeysAndValues()
    {
        var original = new Dictionary<string, int>
        {
            ["first"] = 1,
            ["second"] = 2,
            ["third"] = 3,
        };

        var result = original.Reverse();

        Assert.Equal(3, result.Count);
        Assert.Equal("first", result[1]);
        Assert.Equal("second", result[2]);
        Assert.Equal("third", result[3]);
    }

    [Fact]
    public void Reverse_ShouldOverwriteDuplicateValuesWithLatestKey()
    {
        var original = new Dictionary<string, int>
        {
            ["first"] = 1,
            ["duplicateOne"] = 2,
            ["duplicateTwo"] = 2,
        };

        var result = original.Reverse();

        Assert.Equal(2, result.Count);
        Assert.Equal("duplicateTwo", result[2]);
        Assert.False(result.Values.Contains("duplicateOne"));
    }

    [Fact]
    public void ContainsAll_ShouldBeCaseInsensitiveAndReturnFalseForMissingValues()
    {
        const string source = "Hello World";
        var values = new List<string> { "hello", "WORLD" };
        var missingValues = new List<string> { "hello", "world", "goodbye" };

        Assert.True(source.ContainsAll(values));
        Assert.False(source.ContainsAll(missingValues));
    }

    [Fact]
    public void ContainsAny_ShouldBeCaseInsensitiveAndDetectNegativeCase()
    {
        const string source = "Alpha Beta";
        var values = new List<string> { "gamma", "BETA" };
        var missingValues = new List<string> { "gamma", "delta" };

        Assert.True(source.ContainsAny(values));
        Assert.False(source.ContainsAny(missingValues));
    }

    [Fact]
    public void Batch_ShouldThrowWhenSizeIsNotPositive()
    {
        var source = new List<int> { 1, 2, 3 };

        Assert.Throws<ArgumentOutOfRangeException>(() => source.Batch(0).ToList());
        Assert.Throws<ArgumentOutOfRangeException>(() => source.Batch(-1).ToList());
    }

    [Fact]
    public void Batch_ShouldYieldPartialFinalBatch()
    {
        var source = new List<int> { 1, 2, 3, 4, 5 };

        var batches = source.Batch(2).Select(batch => batch.ToList()).ToList();

        Assert.Equal(3, batches.Count);
        Assert.Equal(new List<int> { 1, 2 }, batches[0]);
        Assert.Equal(new List<int> { 3, 4 }, batches[1]);
        Assert.Equal(new List<int> { 5 }, batches[2]);
    }
}
