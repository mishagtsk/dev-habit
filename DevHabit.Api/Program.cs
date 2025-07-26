using System.Diagnostics.CodeAnalysis;
using Asp.Versioning.ApiExplorer;
using DevHabit.Api;
using DevHabit.Api.Endpoints;
using DevHabit.Api.Extensions;
using DevHabit.Api.Middleware;
using Scalar.AspNetCore;
using CorsOptions = DevHabit.Api.Settings.CorsOptions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder
    .AddApiServices()
    .AddErrorHandling()
    .AddDatabase()
    .AddObservability()
    .AddApplicationServices()
    .AddAuthenticationServices()
    .AddCorsPolicy()
    .AddBackgroundJobs()
    .AddRateLimiting();


WebApplication app = builder.Build();

app.MapHabitEndpoints();
// This is pure swagger
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    foreach (ApiVersionDescription description in app.DescribeApiVersions())
    {
        options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName);
    }
});

app.MapScalarApiReference(options =>
{
    options.WithOpenApiRoutePattern("/swagger/1.0/swagger.json");
});

if (app.Environment.IsDevelopment())
{
    /*This is openAPi + swagger specification
    app.MapOpenApi();
    
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });
    */
    

    await app.ApplyMigrationsAsync();

    await app.SeedInitialDataAsync();
}

app.UseHttpsRedirection();

app.UseExceptionHandler();

app.UseCors(CorsOptions.PolicyName);

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.UseMiddleware<ETagMiddleware>();

app.MapControllers();

await app.RunAsync();

[ExcludeFromCodeCoverage]
public partial class Program;
