using System.Net;
using System.Net.Http.Json;
using Shouldly;

namespace WarehouseManager.IntegrationTests.Products;

public sealed class ProductsTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task CreateProduct_Should_ReturnCreatedWithId_ThenAppearInGetProducts()
    {
        var request = new { Code = $"SKU-{Guid.NewGuid():N}", Description = "Test product" };

        HttpResponseMessage createResponse = await HttpClient.PostAsJsonAsync("products", request);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        ProductResponse? created = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();
        created.ShouldNotBeNull();
        created.Id.ShouldNotBe(Guid.Empty);
        created.Code.ShouldBe(request.Code);

        HttpResponseMessage getResponse = await HttpClient.GetAsync("products");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        List<ProductResponse>? products = await getResponse.Content.ReadFromJsonAsync<List<ProductResponse>>();

        products.ShouldNotBeNull();
        products.ShouldContain(p => p.Id == created.Id && p.Code == request.Code && p.Description == request.Description);
    }

    [Fact]
    public async Task CreateProduct_Should_ReturnConflict_WhenCodeAlreadyExists()
    {
        var request = new { Code = $"SKU-{Guid.NewGuid():N}", Description = "Original" };

        (await HttpClient.PostAsJsonAsync("products", request)).StatusCode.ShouldBe(HttpStatusCode.Created);

        HttpResponseMessage duplicateResponse = await HttpClient.PostAsJsonAsync("products", request);

        duplicateResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateProduct_Should_ReturnBadRequest_WhenCodeIsEmpty()
    {
        var request = new { Code = string.Empty, Description = "No code" };

        HttpResponseMessage response = await HttpClient.PostAsJsonAsync("products", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private sealed record ProductResponse(Guid Id, string Code, string Description);
}
