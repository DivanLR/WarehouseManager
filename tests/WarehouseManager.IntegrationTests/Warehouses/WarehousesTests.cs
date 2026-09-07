using System.Net;
using System.Net.Http.Json;
using Shouldly;

namespace WarehouseManager.IntegrationTests.Warehouses;

public sealed class WarehousesTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task CreateWarehouse_Should_ReturnCreated_ThenAppearInGetWarehouses()
    {
        var request = new { Code = $"WH-{Guid.NewGuid():N}", Name = "Test depot" };

        var createResponse = await HttpClient.PostAsJsonAsync("api/warehouses", request);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await createResponse.Content.ReadFromJsonAsync<SuccessResponse>())!.Message.ShouldBe("Warehouse created.");

        var getResponse = await HttpClient.GetAsync("api/warehouses");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var warehouses = await getResponse.Content.ReadFromJsonAsync<List<WarehouseResponse>>();

        warehouses.ShouldNotBeNull();
        warehouses.ShouldContain(w => w.Id != Guid.Empty && w.Code == request.Code && w.Name == request.Name);
    }

    [Fact]
    public async Task CreateWarehouse_Should_ReturnConflict_WhenCodeAlreadyExists()
    {
        var request = new { Code = $"WH-{Guid.NewGuid():N}", Name = "Original" };

        (await HttpClient.PostAsJsonAsync("api/warehouses", request)).StatusCode.ShouldBe(HttpStatusCode.Created);

        var duplicateResponse = await HttpClient.PostAsJsonAsync("api/warehouses", request);

        duplicateResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateWarehouse_Should_ReturnConflict_WhenCodeDiffersOnlyByCase()
    {
        var code = $"wh-{Guid.NewGuid():N}";

        (await HttpClient.PostAsJsonAsync("api/warehouses", new { Code = code, Name = "Original" }))
            .StatusCode.ShouldBe(HttpStatusCode.Created);

        var duplicateResponse = await HttpClient.PostAsJsonAsync(
            "api/warehouses",
            new { Code = code.ToUpperInvariant(), Name = "Same code, different case" });

        duplicateResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateWarehouse_Should_ReturnBadRequest_WhenCodeIsEmpty()
    {
        var request = new { Code = string.Empty, Name = "No code" };

        var response = await HttpClient.PostAsJsonAsync("api/warehouses", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private sealed record WarehouseResponse(Guid Id, string Code, string Name);

    private sealed record SuccessResponse(string Message);
}
