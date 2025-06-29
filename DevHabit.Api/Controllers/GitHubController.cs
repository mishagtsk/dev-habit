using System.Net.Mime;
using DevHabit.Api.DTOs.Common;
using DevHabit.Api.DTOs.GitHub;
using DevHabit.Api.Entities;
using DevHabit.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevHabit.Api.Controllers;

[Authorize(Roles = Roles.Member)]
[ApiController]
[Route("[controller]")]
[Produces(
    MediaTypeNames.Application.Json,
    CustomMediaTypeNames.Application.JsonV1,
    CustomMediaTypeNames.Application.HateoasJson,
    CustomMediaTypeNames.Application.HateoasJsonV1)]
public class GitHubController(
    GitHubAccessTokenService gitHubAccessTokenService,
    RefitGitHubService gitHubService,
    UserContext userContext,
    LinkService linkService) : ControllerBase
{
    [HttpPut("personal-access-token")]
    public async Task<IActionResult> StoreAccessToken(
        StoreGitHubAccessTokenDto storeGitHubAccessTokenDto,
        IValidator<StoreGitHubAccessTokenDto> validator)
    {
        await validator.ValidateAndThrowAsync(storeGitHubAccessTokenDto);

        string? userId = await userContext.GetUserIdAsync();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        await gitHubAccessTokenService.StoreAsync(userId, storeGitHubAccessTokenDto);

        return NoContent();
    }
    
    [HttpDelete("personal-access-token")]
    public async Task<IActionResult> RevokeAccessToken(CancellationToken cancellationToken = default)
    {
        string? userId = await userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }
        
        await gitHubAccessTokenService.RevokeAsync(userId, cancellationToken);

        return NoContent();
    }

    [HttpGet("profile")]
    public async Task<ActionResult<GitHubUserProfileDto>> GetUserProfile([FromHeader] AcceptHeaderDto acceptHeader,
        CancellationToken cancellationToken = default)
    {
        string? userId = await userContext.GetUserIdAsync(CancellationToken.None);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        string? accessToken = await gitHubAccessTokenService.GetAsync(userId, cancellationToken);

        if (string.IsNullOrEmpty(accessToken))
        {
            return NotFound();
        }

        GitHubUserProfileDto? userProfile =
            await gitHubService.GetUserProfileAsync(accessToken, CancellationToken.None);

        if (userProfile == null)
        {
            return NotFound();
        }

        if (acceptHeader.IncludeLinks)
        {
            userProfile.Links =
            [
                linkService.Create(nameof(GetUserProfile), "self", HttpMethods.Get),
                linkService.Create(nameof(StoreAccessToken), "store-token", HttpMethods.Put),
                linkService.Create(nameof(RevokeAccessToken), "revoke-token", HttpMethods.Delete)
            ];
        }

        return Ok(userProfile);
    }

    [HttpGet("events")]
    public async Task<ActionResult<IReadOnlyList<GitHubEventDto>>> GetUserEvents()
    {
        string? userId = await userContext.GetUserIdAsync();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        string? accessToken = await gitHubAccessTokenService.GetAsync(userId);
        if (accessToken is null)
        {
            return Unauthorized();
        }

        GitHubUserProfileDto? profile = await gitHubService.GetUserProfileAsync(accessToken);

        if (profile is null)
        {
            return NotFound();
        }

        IReadOnlyList<GitHubEventDto>? events = await gitHubService.GetUserEventsAsync(
            profile.Login,
            accessToken);

        if (events is null)
        {
            return NotFound();
        }

        return Ok(events);
    }
}
