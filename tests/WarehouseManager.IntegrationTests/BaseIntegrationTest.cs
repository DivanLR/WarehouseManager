using System.Net.Http.Headers;

namespace WarehouseManager.IntegrationTests;

[Collection(nameof(IntegrationTestCollection))]
public abstract class BaseIntegrationTest
{
    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
    {
        Factory = factory;

        HttpClient = factory.CreateClient();
        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.AccessToken);
    }

    protected HttpClient HttpClient { get; }

    protected IntegrationTestWebAppFactory Factory { get; }
}
