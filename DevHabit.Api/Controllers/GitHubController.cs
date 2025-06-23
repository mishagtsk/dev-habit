using System.Net.Mime;
using DevHabit.Api.DTOs.Common;
using DevHabit.Api.DTOs.GitHub;
using DevHabit.Api.Entities;
using DevHabit.Api.Services;
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
    public async Task<IActionResult> StoreAccessToken(StoreGitHubAccessTokenDto storeAccessTokenDto,
        CancellationToken cancellationToken = default)
    {
        string? userId = await userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }
        
        await gitHubAccessTokenService.StoreAsync(userId, storeAccessTokenDto, cancellationToken);

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
}
