using System.Net;
using System.Net.Http.Json;
using Shouldly;

namespace WarehouseManager.IntegrationTests.Orders;

public sealed class OrdersTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task CreateOrder_Should_MoveStockFromSourceToDestination()
    {
        var product = await CreateProductAsync();
        var source = await CreateWarehouseAsync();
        var destination = await CreateWarehouseAsync();
        await AddStockAsync(source, product, 30);

        var response = await HttpClient.PostAsJsonAsync("orders", new { ProductCode = product, SourceWarehouseCode = source, DestinationWarehouseCode = destination, Quantity = 10 });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await response.Content.ReadFromJsonAsync<SuccessResponse>())!.Message.ShouldBe("Order created.");

        var stock = await HttpClient.GetFromJsonAsync<List<StockResponse>>($"stock?productCode={product}");
        stock.ShouldNotBeNull();
        stock.Single(s => s.WarehouseCode == source).Quantity.ShouldBe(20);
        stock.Single(s => s.WarehouseCode == destination).Quantity.ShouldBe(10);
    }

    [Fact]
    public async Task CreateOrder_Should_AddToExistingDestinationStock()
    {
        var product = await CreateProductAsync();
        var source = await CreateWarehouseAsync();
        var destination = await CreateWarehouseAsync();
        await AddStockAsync(source, product, 30);
        await AddStockAsync(destination, product, 5);

        (await HttpClient.PostAsJsonAsync("orders", new { ProductCode = product, SourceWarehouseCode = source, DestinationWarehouseCode = destination, Quantity = 10 })).StatusCode.ShouldBe(HttpStatusCode.Created);

        var stock = await HttpClient.GetFromJsonAsync<List<StockResponse>>($"stock?productCode={product}");
        stock.ShouldNotBeNull();
        stock.Single(s => s.WarehouseCode == destination).Quantity.ShouldBe(15);
    }

    [Fact]
    public async Task CreateOrder_Should_ReturnBadRequest_AndLeaveStockUntouched_WhenInsufficientStock()
    {
        var product = await CreateProductAsync();
        var source = await CreateWarehouseAsync();
        var destination = await CreateWarehouseAsync();
        await AddStockAsync(source, product, 5);

        var response = await HttpClient.PostAsJsonAsync("orders", new { ProductCode = product, SourceWarehouseCode = source, DestinationWarehouseCode = destination, Quantity = 10 });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("Orders.InsufficientStock");
        body.ShouldContain("has 5");

        var stock = await HttpClient.GetFromJsonAsync<List<StockResponse>>($"stock?productCode={product}");
        stock.ShouldNotBeNull();
        stock.Single().WarehouseCode.ShouldBe(source);
        stock.Single().Quantity.ShouldBe(5);
    }

    [Fact]
    public async Task CreateOrder_Should_ReturnBadRequest_WhenSourceHasNoStockRow()
    {
        var product = await CreateProductAsync();
        var source = await CreateWarehouseAsync();
        var destination = await CreateWarehouseAsync();

        var response = await HttpClient.PostAsJsonAsync("orders", new { ProductCode = product, SourceWarehouseCode = source, DestinationWarehouseCode = destination, Quantity = 1 });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).ShouldContain("has 0");
    }

    [Fact]
    public async Task CreateOrder_Should_ReturnBadRequest_WhenSourceEqualsDestination()
    {
        var response = await HttpClient.PostAsJsonAsync("orders", new { ProductCode = "SKU-001", SourceWarehouseCode = "WH-001", DestinationWarehouseCode = "WH-001", Quantity = 1 });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).ShouldContain("must differ");
    }

    [Fact]
    public async Task CreateOrder_Should_ReturnNotFound_WhenProductDoesNotExist()
    {
        var source = await CreateWarehouseAsync();
        var destination = await CreateWarehouseAsync();

        var response = await HttpClient.PostAsJsonAsync("orders", new { ProductCode = "SKU-DOES-NOT-EXIST", SourceWarehouseCode = source, DestinationWarehouseCode = destination, Quantity = 1 });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).ShouldContain("Orders.ProductNotFound");
    }

    [Fact]
    public async Task CreateOrder_Should_ReturnNotFound_WhenDestinationWarehouseDoesNotExist()
    {
        var product = await CreateProductAsync();
        var source = await CreateWarehouseAsync();

        var response = await HttpClient.PostAsJsonAsync("orders", new { ProductCode = product, SourceWarehouseCode = source, DestinationWarehouseCode = "WH-DOES-NOT-EXIST", Quantity = 1 });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).ShouldContain("Orders.DestinationWarehouseNotFound");
    }

    private async Task<string> CreateWarehouseAsync()
    {
        var code = $"WH-{Guid.NewGuid():N}";
        (await HttpClient.PostAsJsonAsync("warehouses", new { Code = code, Name = "Order test depot" })).EnsureSuccessStatusCode();

        return code;
    }

    private async Task<string> CreateProductAsync()
    {
        var code = $"SKU-{Guid.NewGuid():N}";
        (await HttpClient.PostAsJsonAsync("products", new { Code = code, Description = "Order test product" })).EnsureSuccessStatusCode();

        return code;
    }

    private async Task AddStockAsync(string warehouseCode, string productCode, int quantity) =>
        (await HttpClient.PostAsJsonAsync("stock", new { WarehouseCode = warehouseCode, ProductCode = productCode, Quantity = quantity })).EnsureSuccessStatusCode();

    private sealed record StockResponse(Guid Id, Guid WarehouseId, string WarehouseCode, Guid ProductId, string ProductCode, int Quantity);

    private sealed record SuccessResponse(string Message);
}
