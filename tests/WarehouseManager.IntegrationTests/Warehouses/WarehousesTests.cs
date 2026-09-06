using System.Net;
using System.Net.Http.Json;
using Shouldly;

namespace WarehouseManager.IntegrationTests.Warehouses;

public sealed class WarehousesTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task CreateWarehouse_Should_ReturnCreatedWithId_ThenAppearInGetWarehouses()
    {
        var request = new { Code = $"WH-{Guid.NewGuid():N}", Name = "Test depot" };

        HttpResponseMessage createResponse = await HttpClient.PostAsJsonAsync("warehouses", request);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        WarehouseResponse? created = await createResponse.Content.ReadFromJsonAsync<WarehouseResponse>();
        created.ShouldNotBeNull();
        created.Id.ShouldNotBe(Guid.Empty);
        created.Code.ShouldBe(request.Code);

        HttpResponseMessage getResponse = await HttpClient.GetAsync("warehouses");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        List<WarehouseResponse>? warehouses = await getResponse.Content.ReadFromJsonAsync<List<WarehouseResponse>>();

        warehouses.ShouldNotBeNull();
        warehouses.ShouldContain(w => w.Id == created.Id && w.Code == request.Code && w.Name == request.Name);
    }

    [Fact]
    public async Task CreateWarehouse_Should_ReturnConflict_WhenCodeAlreadyExists()
    {
        var request = new { Code = $"WH-{Guid.NewGuid():N}", Name = "Original" };

        (await HttpClient.PostAsJsonAsync("warehouses", request)).StatusCode.ShouldBe(HttpStatusCode.Created);

        HttpResponseMessage duplicateResponse = await HttpClient.PostAsJsonAsync("warehouses", request);

        duplicateResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateWarehouse_Should_ReturnBadRequest_WhenCodeIsEmpty()
    {
        var request = new { Code = string.Empty, Name = "No code" };

        HttpResponseMessage response = await HttpClient.PostAsJsonAsync("warehouses", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private sealed record WarehouseResponse(Guid Id, string Code, string Name);
}
