namespace Motorsports.PlaywrightTests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PlaywrightCollection : ICollectionFixture<PlaywrightSuiteFixture>
{
    public const string Name = "Playwright browser regression";
}
