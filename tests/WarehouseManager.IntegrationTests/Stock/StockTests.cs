using System.Net;
using System.Net.Http.Json;
using Shouldly;

namespace WarehouseManager.IntegrationTests.Stock;

public sealed class StockTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task AddStock_Should_ReturnNoContent_ThenAppearInBothFilters()
    {
        var warehouseCode = await CreateWarehouseAsync();
        var productCode = await CreateProductAsync();

        var addResponse = await HttpClient.PostAsJsonAsync("api/stock", new { WarehouseCode = warehouseCode, ProductCode = productCode, Quantity = 25 });
        addResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await addResponse.Content.ReadFromJsonAsync<SuccessResponse>())!.Message.ShouldBe("Stock added.");

        var byWarehouse = await HttpClient.GetFromJsonAsync<List<StockResponse>>($"api/stock?warehouseCode={warehouseCode}");
        byWarehouse.ShouldNotBeNull();
        byWarehouse.ShouldContain(s => s.ProductCode == productCode && s.Quantity == 25);

        var byProduct = await HttpClient.GetFromJsonAsync<List<StockResponse>>($"api/stock?productCode={productCode}");
        byProduct.ShouldNotBeNull();
        byProduct.ShouldContain(s => s.WarehouseCode == warehouseCode && s.Quantity == 25);

        var byBoth = await HttpClient.GetFromJsonAsync<List<StockResponse>>($"api/stock?warehouseCode={warehouseCode}&productCode={productCode}");
        byBoth.ShouldNotBeNull();
        byBoth.Single().Quantity.ShouldBe(25);
    }

    [Fact]
    public async Task AddStock_Should_AccumulateQuantity_WhenCalledAgainForSamePair()
    {
        var warehouseCode = await CreateWarehouseAsync();
        var productCode = await CreateProductAsync();

        (await HttpClient.PostAsJsonAsync("api/stock", new { WarehouseCode = warehouseCode, ProductCode = productCode, Quantity = 10 })).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await HttpClient.PostAsJsonAsync("api/stock", new { WarehouseCode = warehouseCode, ProductCode = productCode, Quantity = 5 })).StatusCode.ShouldBe(HttpStatusCode.OK);

        var stock = await HttpClient.GetFromJsonAsync<List<StockResponse>>($"api/stock?warehouseCode={warehouseCode}&productCode={productCode}");

        stock.ShouldNotBeNull();
        stock.Single().Quantity.ShouldBe(15);
    }

    [Fact]
    public async Task AddStock_Should_ReturnNotFound_WhenWarehouseCodeDoesNotExist()
    {
        var productCode = await CreateProductAsync();

        var response = await HttpClient.PostAsJsonAsync("api/stock", new { WarehouseCode = "WH-DOES-NOT-EXIST", ProductCode = productCode, Quantity = 1 });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).ShouldContain("Stock.WarehouseNotFound");
    }

    [Fact]
    public async Task AddStock_Should_ReturnNotFound_WhenProductCodeDoesNotExist()
    {
        var warehouseCode = await CreateWarehouseAsync();

        var response = await HttpClient.PostAsJsonAsync("api/stock", new { WarehouseCode = warehouseCode, ProductCode = "SKU-DOES-NOT-EXIST", Quantity = 1 });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).ShouldContain("Stock.ProductNotFound");
    }

    [Fact]
    public async Task AddStock_Should_ReturnBadRequest_WhenQuantityIsZero()
    {
        var response = await HttpClient.PostAsJsonAsync("api/stock", new { WarehouseCode = "WH-001", ProductCode = "SKU-001", Quantity = 0 });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetStock_Should_ReturnNotFound_WhenWarehouseCodeDoesNotExist()
    {
        var response = await HttpClient.GetAsync("api/stock?warehouseCode=WH-DOES-NOT-EXIST");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetStock_Should_ReturnNotFound_WhenProductCodeDoesNotExist()
    {
        var response = await HttpClient.GetAsync("api/stock?productCode=SKU-DOES-NOT-EXIST");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetStock_Should_ReturnEmptyList_WhenWarehouseHasNoStock()
    {
        var warehouseCode = await CreateWarehouseAsync();

        var stock = await HttpClient.GetFromJsonAsync<List<StockResponse>>($"api/stock?warehouseCode={warehouseCode}");

        stock.ShouldNotBeNull();
        stock.ShouldBeEmpty();
    }

    private async Task<string> CreateWarehouseAsync()
    {
        var code = $"WH-{Guid.NewGuid():N}";
        (await HttpClient.PostAsJsonAsync("api/warehouses", new { Code = code, Name = "Stock test depot" })).EnsureSuccessStatusCode();

        return code;
    }

    private async Task<string> CreateProductAsync()
    {
        var code = $"SKU-{Guid.NewGuid():N}";
        (await HttpClient.PostAsJsonAsync("api/products", new { Code = code, Description = "Stock test product" })).EnsureSuccessStatusCode();

        return code;
    }

    private sealed record StockResponse(Guid Id, Guid WarehouseId, string WarehouseCode, Guid ProductId, string ProductCode, int Quantity);

    private sealed record SuccessResponse(string Message);
}
