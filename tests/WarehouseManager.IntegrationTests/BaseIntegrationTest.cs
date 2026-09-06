namespace WarehouseManager.IntegrationTests;

[Collection(nameof(IntegrationTestCollection))]
public abstract class BaseIntegrationTest(IntegrationTestWebAppFactory factory)
{
    protected HttpClient HttpClient { get; } = factory.CreateClient();
}
