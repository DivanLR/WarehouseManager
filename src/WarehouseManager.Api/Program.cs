using System.Reflection;
using Scalar.AspNetCore;
using WarehouseManager.Api.Behaviors;
using WarehouseManager.Api.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddWebHostInfrastructure(builder.Configuration);

builder.Services.AddEndpoints(Assembly.GetExecutingAssembly());

WebApplication app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();

app.UseAuthentication();
app.UseAuthorization();
app.MapEndpoints(app.MapGroup("api"));

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference(options => options.WithTitle("WarehouseManager API")).AllowAnonymous();

    app.ApplyMigrations();
}

app.MapHealthChecks("health").AllowAnonymous();

app.UseExceptionHandler();

await app.RunAsync();

// REMARK: Required for integration tests to work.
namespace WarehouseManager.Api
{
    public partial class Program;
}
