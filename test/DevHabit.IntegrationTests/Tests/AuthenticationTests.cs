using System.Net;
using System.Net.Http.Json;
using DevHabit.Api.DTOs.Auth;
using DevHabit.IntegrationTests.Infrastructure;

namespace DevHabit.IntegrationTests.Tests;

public sealed class AuthenticationTests(DevHabitWebAppFactory factory) : IntegrationTestFixture(factory)
{
    [Fact]
    public async Task Register_ShouldSucceed_WithValidParameter()
    {
        // Arrange
        var dto = new RegisterUserDto
        {
            Email = "register@test.com",
            Name = "register@test.com",
            Password = "Test123!",
            ConfirmPassword = "Test123!"
        };
        
        HttpClient client = CreateClient();

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(Routes.Auth.Register, dto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
 
    [Fact]
    public async Task Register_ShouldAccessTokens_WithValidParameter()
    {
        // Arrange
        var dto = new RegisterUserDto
        {
            Email = "register1@test.com",
            Name = "register1@test.com",
            Password = "Test123!",
            ConfirmPassword = "Test123!"
        };
        
        HttpClient client = CreateClient();

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(Routes.Auth.Register, dto);
        response.EnsureSuccessStatusCode();

        // Assert
        AccessTokensDto? accessTokenDto = await response.Content.ReadFromJsonAsync<AccessTokensDto>();
        Assert.NotNull(accessTokenDto);
    }
}
