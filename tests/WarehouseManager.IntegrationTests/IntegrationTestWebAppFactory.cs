using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;
using WarehouseManager.Api;

namespace WarehouseManager.IntegrationTests;

public sealed class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("warehouse-manager")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public string AccessToken { get; private set; } = string.Empty;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Database", _dbContainer.GetConnectionString());

        builder.UseSetting("Jwt:Secret", "integration-tests-signing-key-not-a-real-secret");
        builder.UseSetting("Jwt:Issuer", "WarehouseManager");
        builder.UseSetting("Jwt:Audience", "WarehouseManager");
        builder.UseSetting("Jwt:ExpirationInMinutes", "60");
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        using var client = CreateClient();
        var credentials = new { Email = "tests@example.com", Password = "integration-tests-password" };

        (await client.PostAsJsonAsync("api/auth/register", credentials)).EnsureSuccessStatusCode();

        var loginResponse = await client.PostAsJsonAsync("api/auth/login", credentials);
        loginResponse.EnsureSuccessStatusCode();

        AccessToken = (await loginResponse.Content.ReadFromJsonAsync<AuthTokenResponse>())!.AccessToken;
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
        await base.DisposeAsync();
    }

    private sealed record AuthTokenResponse(string AccessToken);
}
