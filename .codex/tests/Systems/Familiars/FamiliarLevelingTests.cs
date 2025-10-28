using Bloodcraft.Tests;
using Xunit;

namespace Bloodcraft.Tests.Systems.Familiars;

[Collection(FamiliarSystemTestCollection.CollectionName)]
public class FamiliarLevelingTests : TestHost
{
}

public static class FamiliarSystemTestCollection
{
    public const string CollectionName = "Familiars System Tests";
}

[CollectionDefinition(FamiliarSystemTestCollection.CollectionName, DisableParallelization = true)]
public sealed class FamiliarSystemTestCollectionDefinition
{
}
