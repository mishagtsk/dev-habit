using System.Security.Claims;
using DevHabit.Api.Database;
using DevHabit.Api.DTOs.Common;
using DevHabit.Api.DTOs.Users;
using DevHabit.Api.Entities;
using DevHabit.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;

[Authorize(Roles = Roles.Member)]
[ApiController] 
[Route("users")]
public sealed class UsersController(ApplicationDbContext dbContext, UserContext userContext, 
    LinkService linkService) : ControllerBase
{
    [HttpGet("{id}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<UserDto>> GetUserById(string id, CancellationToken cancellationToken)
    {
        string? userId = await userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        if (id != userId)
        {
            return Forbid();
        }
        
        UserDto? user = await dbContext.Users
            .Where(u => u.Id == id)
            .Select(UserQueries.ProjectToDto())
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUser([FromHeader] AcceptHeaderDto acceptHeaderDto,
        CancellationToken cancellationToken)
    {
        string? userId = await userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        UserDto? user = await dbContext.Users
            .Where(u => u.Id == userId)
            .Select(UserQueries.ProjectToDto())
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        if (acceptHeaderDto.IncludeLinks)
        {
            user.Links = CreateLinksForUser();
        }

        return Ok(user);
    }

    [HttpPut("me/profile")]
    public async Task<IActionResult> UpdateProfile(UpdateUserProfileDto dto, 
        IValidator<UpdateUserProfileDto> validator, CancellationToken cancellationToken)
    {
        await validator.ValidateAsync(dto, cancellationToken);
        
        string? userId = await userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }
        
        User? user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return NotFound();
        }
        
        user.Name = dto.Name;
        user.UpdatedAtUtc = DateTime.UtcNow;
        
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return NoContent();
    }

    private List<LinkDto> CreateLinksForUser()
    {
        List<LinkDto> links =
        [
            linkService.Create(nameof(GetCurrentUser), "self", HttpMethods.Get),
            linkService.Create(nameof(UpdateProfile), "update-profile", HttpMethods.Put),
        ];
        
        return links;
    }
}
