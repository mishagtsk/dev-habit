using System.Net.Mime;
using DevHabit.Api.Database;
using DevHabit.Api.DTOs.Common;
using DevHabit.Api.DTOs.Tags;
using DevHabit.Api.Entities;
using DevHabit.Api.Services;
using DevHabit.Api.Settings;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DevHabit.Api.Controllers;

[ResponseCache(Duration = 120)]
[Authorize(Roles = Roles.Member)]
[ApiController] 
[Route("tags")]
[Produces(
    MediaTypeNames.Application.Json,
    CustomMediaTypeNames.Application.JsonV1,
    CustomMediaTypeNames.Application.HateoasJson,
    CustomMediaTypeNames.Application.HateoasJsonV1)]
public class TagsController(ApplicationDbContext dbContext, LinkService linkService, UserContext userContext,
    IOptions<TagsOptions> options) 
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<TagsCollectionDto>> GetTags([FromHeader] AcceptHeaderDto acceptHeader,
        CancellationToken cancellationToken)
    {
        string? userId = await userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        List<TagDto> tags = await dbContext
            .Tags
            .Where(t => t.UserId == userId)
            .Select(TagQueries.ProjectToDto())
            .ToListAsync(cancellationToken);

        var tagsCollectionDto = new TagsCollectionDto
        {
            Items = tags
        };
        
        if (acceptHeader.IncludeLinks)
        {
            tagsCollectionDto.Links = CreateLinksForTags(tags.Count, options.Value.MaxAllowedTags);
            foreach (TagDto tagDto in tagsCollectionDto.Items)
            {
                tagDto.Links = CreateLinksForTag(tagDto.Id);
            }
        }

        return Ok(tagsCollectionDto);
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<TagDto>> GetTag(string id, [FromHeader] AcceptHeaderDto acceptHeader, 
        CancellationToken cancellationToken)
    {
        string? userId = await userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }
        
        TagDto? tag = await dbContext
            .Tags
            .Where(h => h.Id == id && h.UserId == userId)
            .Select(TagQueries.ProjectToDto())
            .FirstOrDefaultAsync(cancellationToken);

        if (tag is null)
        {
            return NotFound();
        }

        if (acceptHeader.IncludeLinks)
        {
            tag.Links = CreateLinksForTag(id);
        }
        
        return Ok(tag);
    }

    [HttpPost]
    public async Task<ActionResult<TagDto>> CreateTag(CreateTagDto createTagDto, 
        [FromHeader] AcceptHeaderDto acceptHeader,
        IValidator<CreateTagDto> validator,
        CancellationToken cancellationToken)
    {
        string? userId = await userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }
        
        await validator.ValidateAndThrowAsync(createTagDto, cancellationToken);

        if (await dbContext.Tags.CountAsync(t => t.UserId == userId, cancellationToken) >= options.Value.MaxAllowedTags)
        {
            return Problem(
                detail: "Reached the maximum number of allowed tags",
                statusCode: StatusCodes.Status400BadRequest);
        }
        
        Tag tag = createTagDto.ToEntity(userId);

        if (await dbContext.Tags.AnyAsync(t => t.UserId == userId && t.Name == tag.Name, cancellationToken))
        {
            return Problem(detail: $"The tag '{tag.Name}' already exists", statusCode: StatusCodes.Status409Conflict);
        }

        dbContext.Tags.Add(tag);

        await dbContext.SaveChangesAsync(cancellationToken);

        TagDto tagDto = tag.ToDto();

        if (acceptHeader.IncludeLinks)
        {
            tagDto.Links = CreateLinksForTag(tag.Id);
        }
        
        return CreatedAtAction(nameof(GetTag), new { id = tagDto.Id }, tagDto);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateTag(string id, UpdateTagDto updateTagDto, InMemoryEtagStore inMemoryEtagStore,
        IValidator<UpdateTagDto> validator, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(updateTagDto, cancellationToken);
        
        string? userId = await userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        Tag? tag = await dbContext.Tags.FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId, cancellationToken);

        if (tag is null)
        {
            return NotFound();
        }

        tag.UpdateFromDto(updateTagDto);

        await dbContext.SaveChangesAsync(cancellationToken);
        
        inMemoryEtagStore.SetETag(Request.Path.Value!, tag.ToDto());

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTag(string id, CancellationToken cancellationToken)
    {
        string? userId = await userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        Tag? tag = await dbContext.Tags.FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId, cancellationToken);

        if (tag is null)
        {
            return NotFound();
        }

        dbContext.Tags.Remove(tag);

        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
    
    private List<LinkDto> CreateLinksForTags(int tagsCount, int maxAllowedTags)
    {
        List<LinkDto> links =
        [
            linkService.Create(nameof(GetTags), "self", HttpMethods.Get),
        ];

        if (tagsCount < maxAllowedTags)
        {
            
            links.Add(linkService.Create(nameof(CreateTag), "create", HttpMethods.Post));
        }

        return links;
    }

    private List<LinkDto> CreateLinksForTag(string id)
    {
        List<LinkDto> links =
        [
            linkService.Create(nameof(GetTag), "self", HttpMethods.Get, new { id }),
            linkService.Create(nameof(UpdateTag), "update", HttpMethods.Put, new { id }),
            linkService.Create(nameof(DeleteTag), "delete", HttpMethods.Delete, new { id }),
        ];
        
        return links;
    }
}
