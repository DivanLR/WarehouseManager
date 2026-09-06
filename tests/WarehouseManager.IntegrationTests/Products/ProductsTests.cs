using System.Net;
using System.Net.Http.Json;
using Shouldly;

namespace WarehouseManager.IntegrationTests.Products;

public sealed class ProductsTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task CreateProduct_Should_ReturnCreated_ThenAppearInGetProducts()
    {
        var request = new { Code = $"SKU-{Guid.NewGuid():N}", Description = "Test product" };

        var createResponse = await HttpClient.PostAsJsonAsync("products", request);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await createResponse.Content.ReadFromJsonAsync<SuccessResponse>())!.Message.ShouldBe("Product created.");

        var getResponse = await HttpClient.GetAsync("products");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var products = await getResponse.Content.ReadFromJsonAsync<List<ProductResponse>>();

        products.ShouldNotBeNull();
        products.ShouldContain(p => p.Id != Guid.Empty && p.Code == request.Code && p.Description == request.Description);
    }

    [Fact]
    public async Task CreateProduct_Should_ReturnConflict_WhenCodeAlreadyExists()
    {
        var request = new { Code = $"SKU-{Guid.NewGuid():N}", Description = "Original" };

        (await HttpClient.PostAsJsonAsync("products", request)).StatusCode.ShouldBe(HttpStatusCode.Created);

        var duplicateResponse = await HttpClient.PostAsJsonAsync("products", request);

        duplicateResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateProduct_Should_ReturnBadRequest_WhenCodeIsEmpty()
    {
        var request = new { Code = string.Empty, Description = "No code" };

        var response = await HttpClient.PostAsJsonAsync("products", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private sealed record ProductResponse(Guid Id, string Code, string Description);

    private sealed record SuccessResponse(string Message);
}
