using Shouldly;

namespace WarehouseManager.IntegrationTests;

public sealed class CorrelationIdTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private const string HeaderName = "X-Correlation-Id";

    [Fact]
    public async Task Response_Should_CarryCorrelationIdHeader_WhenNoneSupplied()
    {
        HttpResponseMessage response = await HttpClient.GetAsync("api/products");

        response.Headers.TryGetValues(HeaderName, out IEnumerable<string>? values).ShouldBeTrue();
        values.ShouldNotBeNull();
        values.Single().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Response_Should_EchoSuppliedCorrelationId()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/products");
        request.Headers.Add(HeaderName, "client-supplied-123");

        HttpResponseMessage response = await HttpClient.SendAsync(request);

        response.Headers.GetValues(HeaderName).Single().ShouldBe("client-supplied-123");
    }
}
