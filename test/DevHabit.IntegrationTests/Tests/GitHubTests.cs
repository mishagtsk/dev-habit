using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Net.Mime;
using DevHabit.Api.DTOs.GitHub;
using DevHabit.IntegrationTests.Infrastructure;
using Newtonsoft.Json;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace DevHabit.IntegrationTests.Tests;

public sealed class GitHubTests(DevHabitWebAppFactory factory) : IntegrationTestFixture(factory)
{
    public const string TestAccessToken = "gho_test123456789";

    private static readonly GitHubUserProfileDto User = new(
        Login: "testuser",
        Name: "Test User",
        AvatarUrl: "https://github.com/testuser.png",
        Bio: "Test bio",
        PublicRepos: 10,
        Followers: 20,
        Following: 30
    );

    public static readonly GitHubEventDto TestEvent = new(
        Id: "1234567890",
        Type: "PushEvent",
        Actor: new GitHubActorDto(
            Id: 1,
            Login: "testuser",
            DisplayLogin: "testuser",
            AvatarUrl: "https://github.com/testuser.png"),
        Repository: new GitHubRepositoryDto(
            Id: 1,
            Name: "testuser/repo",
            Url: "https://api.github.com/repos/testuser/repo"),
        Payload: new GitHubPayloadDto(
            Action: "test-action",
            Ref: "refs/heads/main",
            Commits:
            [
                new GitHubCommitDto(
                    Sha: "abc123",
                    Message: "Test commit",
                    Url: "https://github.com/testuser/repo/commit/abc123")
            ]),
        IsPublic: true,
        CreatedAt: DateTime.Parse("2025-01-01T00:00:00Z", CultureInfo.InvariantCulture)
    );

    [Fact]
    public async Task GetProfile_ShouldReturnUserProfile_WhenAccessTokenIsValid()
    {
        // Arrange
        WireMockServer
            .Given(Request.Create()
                .WithPath("/user")
                .WithHeader("Authorization", $"Bearer {TestAccessToken}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", MediaTypeNames.Application.Json)
                .WithBodyAsJson(User));

        HttpClient client = await CreateAuthenticatedClientAsync();

        var dto = new StoreGitHubAccessTokenDto
        {
            AccessToken = TestAccessToken,
            ExpiresInDays = 30
        };

        await client.PutAsJsonAsync(Routes.GitHub.StoreAccessToken, dto);

        // Act
        HttpResponseMessage response = await client.GetAsync(Routes.GitHub.GetProfile);
        response.EnsureSuccessStatusCode();

        // Assert
        GitHubUserProfileDto? profile = JsonConvert.DeserializeObject<GitHubUserProfileDto>(await response.Content.ReadAsStringAsync());

        Assert.NotNull(profile);
        Assert.Equivalent(User, profile);
    }
}
