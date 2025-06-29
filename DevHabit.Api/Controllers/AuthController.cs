using DevHabit.Api.Database;
using DevHabit.Api.DTOs.Auth;
using DevHabit.Api.DTOs.Users;
using DevHabit.Api.Entities;
using DevHabit.Api.Services;
using DevHabit.Api.Settings;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("auth")]
[AllowAnonymous]
public class AuthController(
    UserManager<IdentityUser> userManager,
    ApplicationDbContext applicationDbContext,
    ApplicationIdentityDbContext identityDbContext,
    TokenProvider tokenProvider,
    IOptions<JwtAuthOptions> options) : ControllerBase
{
    private readonly JwtAuthOptions _jwtAuthOptions = options.Value;

    [HttpPost("register")]
    public async Task<ActionResult<AccessTokensDto>> Register(RegisterUserDto registerUserDto,
        IValidator<RegisterUserDto> registerUserDtoValidator)
    {
        await registerUserDtoValidator.ValidateAndThrowAsync(registerUserDto);

        await using IDbContextTransaction transaction = await identityDbContext.Database.BeginTransactionAsync();

        applicationDbContext.Database.SetDbConnection(identityDbContext.Database.GetDbConnection());
        await applicationDbContext.Database.UseTransactionAsync(transaction.GetDbTransaction());

        // Create identity user
        var identityUser = new IdentityUser
        {
            Email = registerUserDto.Email,
            UserName = registerUserDto.Name,
        };

        IdentityResult createUserResult = await userManager.CreateAsync(identityUser, registerUserDto.Password);

        if (!createUserResult.Succeeded)
        {
            var extensions = new Dictionary<string, object?>
                { { "errors", createUserResult.Errors.ToDictionary(e => e.Code, e => e.Description) } };

            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: "Unable to register user. Please try again",
                extensions: extensions);
        }

        IdentityResult addToRoleResult = await userManager.AddToRoleAsync(identityUser, Roles.Member);

        if (!addToRoleResult.Succeeded)
        {
            var extensions = new Dictionary<string, object?>
                { { "errors", addToRoleResult.Errors.ToDictionary(e => e.Code, e => e.Description) } };

            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: "Unable to register user. Please try again",
                extensions: extensions);
        }

        // Create app user
        User user = registerUserDto.ToEntity();
        user.IdentityId = identityUser.Id;

        applicationDbContext.Users.Add(user);

        await applicationDbContext.SaveChangesAsync();

        var tokenRequest = new TokenRequest(identityUser.Id, identityUser.Email, [Roles.Member]);

        AccessTokensDto accessTokens = tokenProvider.Create(tokenRequest);

        var refreshToken = new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = identityUser.Id,
            Token = accessTokens.RefreshToken,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtAuthOptions.RefreshTokenExpirationDays),
        };

        identityDbContext.RefreshTokens.Add(refreshToken);

        await identityDbContext.SaveChangesAsync();

        await transaction.CommitAsync();

        // return
        return Ok(accessTokens);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AccessTokensDto>> Login(LoginUserDto loginUserDto,
        IValidator<LoginUserDto> validator)
    {
        await validator.ValidateAndThrowAsync(loginUserDto);

        IdentityUser? identityUser = await userManager.FindByEmailAsync(loginUserDto.Email);

        if (identityUser == null || !await userManager.CheckPasswordAsync(identityUser, loginUserDto.Password))
        {
            return Unauthorized();
        }

        IList<string> roles = await userManager.GetRolesAsync(identityUser);

        var tokenRequest = new TokenRequest(identityUser.Id, identityUser.Email!, roles);

        AccessTokensDto accessTokens = tokenProvider.Create(tokenRequest);

        var refreshToken = new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = identityUser.Id,
            Token = accessTokens.RefreshToken,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtAuthOptions.RefreshTokenExpirationDays),
        };

        identityDbContext.RefreshTokens.Add(refreshToken);

        await identityDbContext.SaveChangesAsync();

        return Ok(accessTokens);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AccessTokensDto>> Refresh(RefreshTokenDto refreshTokenDto,
        IValidator<RefreshTokenDto> refreshTokenValidator)
    {
        await refreshTokenValidator.ValidateAndThrowAsync(refreshTokenDto);

        RefreshToken? refreshToken = await identityDbContext.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == refreshTokenDto.RefreshToken);

        if (refreshToken == null)
        {
            return Unauthorized();
        }

        if (refreshToken.ExpiresAtUtc < DateTime.UtcNow)
        {
            return Unauthorized();
        }

        IList<string> roles = await userManager.GetRolesAsync(refreshToken.User);

        var tokenRequest = new TokenRequest(refreshToken.User.Id, refreshToken.User.Email!, roles);

        AccessTokensDto accessTokens = tokenProvider.Create(tokenRequest);

        refreshToken.Token = accessTokens.RefreshToken;
        refreshToken.ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtAuthOptions.RefreshTokenExpirationDays);

        await identityDbContext.SaveChangesAsync();

        return Ok(accessTokens);
    }
}
