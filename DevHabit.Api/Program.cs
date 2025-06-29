using System.Diagnostics.CodeAnalysis;
using DevHabit.Api;
using DevHabit.Api.Extensions;
using DevHabit.Api.Middleware;
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

if (app.Environment.IsDevelopment())
{
    /*This is openAPi + swagger specification
    app.MapOpenApi();
    
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });
    */
    
    // This is pure swagger
    app.UseSwagger();
    app.UseSwaggerUI();

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
