using System.Net.Http.Headers;
using System.Net.Http.Json;
using DevHabit.Api.Database;
using DevHabit.Api.DTOs.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WireMock.Server;

namespace DevHabit.IntegrationTests.Infrastructure;

[Collection(nameof(IntegrationTestCollection))]
public abstract class IntegrationTestFixture(DevHabitWebAppFactory factory) : IClassFixture<DevHabitWebAppFactory>
{
    private HttpClient? authorizedCliet;
    protected HttpClient CreateClient() => factory.CreateClient();
    
    protected WireMockServer WireMockServer => factory.GetWireMockServer();

    public async Task<HttpClient> CreateAuthenticatedClientAsync(string email = "test@test.com",
        string password = "Test123!")
    {
        if (authorizedCliet != null)
        {
            return authorizedCliet;
        }
        
        HttpClient client = CreateClient();

        bool userExists;
        using IServiceScope scope = factory.Services.CreateScope();
        await using ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        userExists = await dbContext.Users.AnyAsync(u => u.Email == email);

        if (!userExists)
        {
            HttpResponseMessage registerResponse = await client.PostAsJsonAsync(Routes.Auth.Register,
                new RegisterUserDto
                {
                    Email = email,
                    Name = email,
                    Password = password,
                    ConfirmPassword = password
                });
            
            registerResponse.EnsureSuccessStatusCode();
        }

        HttpResponseMessage loginResponse = await client.PostAsJsonAsync(Routes.Auth.Login,
            new LoginUserDto
            {
                Email = email,
                Password = password
            });
        
        loginResponse.EnsureSuccessStatusCode();
        
        AccessTokensDto? loginResult = await loginResponse.Content.ReadFromJsonAsync<AccessTokensDto>();

        if (loginResult?.AccessToken is null)
        {
            throw new InvalidOperationException("Invalid login response");
        }
        
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult.AccessToken);
        
        authorizedCliet = client;

        return client;
    }
}
