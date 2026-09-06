# Tests

Two test kinds per slice. Both as they compiled in this project for Products.

## Validator unit test

`tests/WarehouseManager.UnitTests/Products/CreateProductCommandValidatorTests.cs`. One failing case per rule, one passing case. No database, no DI.

```csharp
using FluentValidation.Results;
using Shouldly;
using WarehouseManager.Api.Features.Products.CreateProduct;

namespace WarehouseManager.UnitTests.Products;

public class CreateProductCommandValidatorTests
{
    private readonly CreateProductCommandValidator _validator = new();

    [Fact]
    public void Validate_Should_Fail_WhenCodeIsEmpty()
    {
        var command = new CreateProductCommand(string.Empty, "A description");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_Should_Fail_WhenDescriptionIsEmpty()
    {
        var command = new CreateProductCommand("SKU-001", string.Empty);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_Should_Succeed_WhenFieldsAreValid()
    {
        var command = new CreateProductCommand("SKU-001", "A description");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeTrue();
    }
}
```

## Integration test

`tests/WarehouseManager.IntegrationTests/Products/ProductsTests.cs`. One class per entity covering all its endpoints, through real HTTP against a Testcontainers Postgres that `IntegrationTestWebAppFactory` starts once per test collection. Migrations run on startup, so the table exists. Unique values per test (`Guid.NewGuid():N`) because the database is shared across the collection.

```csharp
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

        HttpResponseMessage createResponse = await HttpClient.PostAsJsonAsync("products", request);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        HttpResponseMessage getResponse = await HttpClient.GetAsync("products");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        List<ProductResponse>? products = await getResponse.Content.ReadFromJsonAsync<List<ProductResponse>>();

        products.ShouldNotBeNull();
        products.ShouldContain(p => p.Code == request.Code && p.Description == request.Description);
    }

    [Fact]
    public async Task CreateProduct_Should_ReturnConflict_WhenCodeAlreadyExists()
    {
        var request = new { Code = $"SKU-{Guid.NewGuid():N}", Description = "Original" };

        (await HttpClient.PostAsJsonAsync("products", request)).StatusCode.ShouldBe(HttpStatusCode.Created);

        HttpResponseMessage duplicateResponse = await HttpClient.PostAsJsonAsync("products", request);

        duplicateResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    private sealed record ProductResponse(string Code, string Description);
}
```

The private `ProductResponse` record is deliberate: the test deserialises the wire shape independently of the API's own type, so a change to the contract shows up as a failing test rather than being silently absorbed.

Cover, per endpoint: the happy path, and each `Error` the handler can return (409 for a conflict, 404 for a missing key, 400 for a validation failure by posting an empty required field).

## Running

Unit tests need nothing. Integration tests need Docker Desktop running; without it the fixture fails to start the container and every test in the collection errors, which is expected in that environment, run them once Docker is up.
