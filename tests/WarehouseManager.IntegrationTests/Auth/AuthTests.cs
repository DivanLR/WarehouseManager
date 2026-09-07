using System.Net;
using System.Net.Http.Json;
using Shouldly;

namespace WarehouseManager.IntegrationTests.Auth;

public sealed class AuthTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Register_Should_ReturnCreated_ForANewEmail()
    {
        using var anonymous = Factory.CreateClient();

        var response = await anonymous.PostAsJsonAsync("api/auth/register", NewCredentials());

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await response.Content.ReadFromJsonAsync<SuccessResponse>())!.Message.ShouldBe("Account created.");
    }

    [Fact]
    public async Task Register_Should_ReturnConflict_WhenEmailAlreadyExists()
    {
        using var anonymous = Factory.CreateClient();
        var credentials = NewCredentials();

        (await anonymous.PostAsJsonAsync("api/auth/register", credentials)).StatusCode.ShouldBe(HttpStatusCode.Created);

        var duplicate = await anonymous.PostAsJsonAsync("api/auth/register", credentials);

        duplicate.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await duplicate.Content.ReadAsStringAsync()).ShouldContain("Users.DuplicateEmail");
    }

    [Fact]
    public async Task Register_Should_ReturnValidationErrorsCarryingTheFieldName()
    {
        using var anonymous = Factory.CreateClient();

        var response = await anonymous.PostAsJsonAsync("api/auth/register", new { Email = "not-an-email", Password = "short" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
        problem.ShouldNotBeNull();
        problem.Errors.ShouldNotBeNull();
        problem.Errors.Select(e => e.Code).ShouldContain("Email");
        problem.Errors.Select(e => e.Code).ShouldContain("Password");
    }

    [Fact]
    public async Task Login_Should_ReturnAToken_ForValidCredentials()
    {
        using var anonymous = Factory.CreateClient();
        var credentials = NewCredentials();
        (await anonymous.PostAsJsonAsync("api/auth/register", credentials)).EnsureSuccessStatusCode();

        var response = await anonymous.PostAsJsonAsync("api/auth/login", credentials);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var auth = await response.Content.ReadFromJsonAsync<AuthTokenResponse>();
        auth.ShouldNotBeNull();
        auth.AccessToken.ShouldNotBeNullOrWhiteSpace();
        auth.AccessToken.Split('.').Length.ShouldBe(3);
    }

    [Fact]
    public async Task Login_Should_ReturnUnauthorized_ForTheWrongPassword()
    {
        using var anonymous = Factory.CreateClient();
        var credentials = NewCredentials();
        (await anonymous.PostAsJsonAsync("api/auth/register", credentials)).EnsureSuccessStatusCode();

        var response = await anonymous.PostAsJsonAsync("api/auth/login", new { credentials.Email, Password = "definitely-not-the-password" });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync()).ShouldContain("Users.InvalidCredentials");
    }

    [Fact]
    public async Task Login_Should_ReturnUnauthorized_ForAnUnknownEmail()
    {
        using var anonymous = Factory.CreateClient();

        var response = await anonymous.PostAsJsonAsync("api/auth/login", new { Email = $"nobody-{Guid.NewGuid():N}@example.com", Password = "some-password" });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync()).ShouldContain("Users.InvalidCredentials");
    }

    [Theory]
    [InlineData("api/products")]
    [InlineData("api/warehouses")]
    [InlineData("api/stock")]
    public async Task ProtectedEndpoints_Should_ReturnUnauthorized_WithoutAToken(string route)
    {
        using var anonymous = Factory.CreateClient();

        var response = await anonymous.GetAsync(route);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_Should_Succeed_WithAToken()
    {
        var response = await HttpClient.GetAsync("api/products");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Health_Should_StayPublic()
    {
        using var anonymous = Factory.CreateClient();

        var response = await anonymous.GetAsync("health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static Credentials NewCredentials() => new($"user-{Guid.NewGuid():N}@example.com", "a-valid-password");

    private sealed record Credentials(string Email, string Password);

    private sealed record SuccessResponse(string Message);

    private sealed record AuthTokenResponse(string AccessToken);

    private sealed record ProblemResponse(string Title, int Status, ValidationErrorItem[]? Errors);

    private sealed record ValidationErrorItem(string Code, string Description);
}
