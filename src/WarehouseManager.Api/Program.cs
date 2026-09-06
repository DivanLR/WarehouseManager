using System.Reflection;
using WarehouseManager.Api.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddWebHostInfrastructure(builder.Configuration);

builder.Services.AddEndpoints(Assembly.GetExecutingAssembly());

WebApplication app = builder.Build();

app.MapEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.ApplyMigrations();
}

app.MapHealthChecks("health");

app.UseExceptionHandler();

await app.RunAsync();

// REMARK: Required for integration tests to work.
namespace WarehouseManager.Api
{
    public partial class Program;
}
