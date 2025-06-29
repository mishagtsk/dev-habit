using System.Dynamic;
using System.Net.Mime;
using Asp.Versioning;
using DevHabit.Api.Database;
using DevHabit.Api.DTOs.Common;
using DevHabit.Api.DTOs.Habits;
using DevHabit.Api.Entities;
using DevHabit.Api.Services;
using DevHabit.Api.Services.Sorting;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;

[Authorize(Roles = Roles.Member)]
[ApiController]
[Route("habits")]
[ApiVersion(1.0)]
[Produces(
    MediaTypeNames.Application.Json,
    CustomMediaTypeNames.Application.JsonV1,
    CustomMediaTypeNames.Application.JsonV2,
    CustomMediaTypeNames.Application.HateoasJson,
    CustomMediaTypeNames.Application.HateoasJsonV1,
    CustomMediaTypeNames.Application.HateoasJsonV2)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class HabitsController(ApplicationDbContext dbContext, LinkService linkService, UserContext userContext)
    : ControllerBase
{
    /// <summary>
    /// Retrieves a paginated list of habits
    /// </summary>
    /// <param name="query">Query parameters for filtering and pagination</param>
    /// <param name="sortMappingProvider">Provider for sorting mappings</param>
    /// <param name="dataShapingService">Service for data shaping</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Paginated list of habits</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetHabits([FromQuery] HabitsQueryParameters query,
        SortMappingProvider sortMappingProvider, DataShapingService dataShapingService,
        CancellationToken cancellationToken)
    {
        string? userId = await userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }
        
        if (!sortMappingProvider.ValidateMappings<HabitDto, Habit>(query.Sort))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provided sort parameter is not valid: {query.Sort}.");
        }

        if (!dataShapingService.Validate<HabitDto>(query.Fields))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provided data shaping fields are not valid: {query.Fields}.");
        }

        query.Search = query.Search?.Trim().ToLower();

        SortMapping[] sortMappings = sortMappingProvider.GetMappings<HabitDto, Habit>();

        IQueryable<HabitDto> habitsQuery = dbContext.Habits
            .Where(h => h.UserId == userId)
            .Where(h => query.Search == null ||
                        h.Name.ToLower().Contains(query.Search) ||
                        h.Description != null && h.Description.ToLower().Contains(query.Search))
            .Where(h => query.Type == null || query.Type.Value == h.Type)
            .Where(h => query.Status == null || query.Status.Value == h.Status)
            .ApplySort(query.Sort, sortMappings)
            .Select(HabitQueries.ProjectToDto());

        int totalCount = await habitsQuery.CountAsync(cancellationToken);

        List<HabitDto> habits = await habitsQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var paginationResult = new PaginationResult<ExpandoObject>
        {
            Items = dataShapingService.ShapeCollectionData(
                habits,
                query.Fields,
                query.IncludeLinks ? h => CreateLinksForHabit(h.Id, query.Fields) : null),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };

        if (query.IncludeLinks)
        {
            paginationResult.Links =
                CreateLinksForHabits(query, paginationResult.HasNextPage, paginationResult.HasPreviousPage);
        }
        
        return Ok(paginationResult);
    }

    /// <summary>
    /// Retrieves a specific habit by ID
    /// </summary>
    /// <param name="id">The habit ID</param>
    /// <param name="query">Query parameters for data shaping</param>
    /// <param name="dataShapingService">Service for data shaping</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The requested habit</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [MapToApiVersion(1.0)]
    public async Task<IActionResult> GetHabit(string id,
        [FromQuery] HabitQueryParameters query,
        DataShapingService dataShapingService,
        CancellationToken cancellationToken)
    {
        string? userId = await userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }
        
        if (!dataShapingService.Validate<HabitWithTagsDto>(query.Fields))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provided data shaping fields are not valid: {query.Fields}.");
        }

        HabitWithTagsDto? habitDto = await dbContext.Habits
            .Where(h => h.Id == id && h.UserId == userId)
            .Select(HabitQueries.ProjectToDtoWithTags()).FirstOrDefaultAsync(cancellationToken);

        if (habitDto == null)
        {
            return NotFound();
        }

        ExpandoObject shapedHabitDto = dataShapingService.ShapeData(habitDto, query.Fields);

        if (query.IncludeLinks)
        {
            List<LinkDto> links = CreateLinksForHabit(id, query.Fields);

            shapedHabitDto.TryAdd("links", links);
        }

        return Ok(shapedHabitDto);
    }

    /// <summary>
    /// Retrieves a specific habit by ID with version 2 of the API
    /// </summary>
    /// <param name="id">The habit ID</param>
    /// <param name="query">Query parameters for data shaping</param>
    /// <param name="dataShapingService">Service for data shaping</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The requested habit</returns>
    [HttpGet("{id}")]
    [ApiVersion(2.0)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHabitV2(string id,
        [FromQuery] HabitQueryParameters query,
        DataShapingService dataShapingService,
        CancellationToken cancellationToken)
    {
        string? userId = await userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }
        
        if (!dataShapingService.Validate<HabitWithTagsDtoV2>(query.Fields))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provided data shaping fields are not valid: {query.Fields}.");
        }

        HabitWithTagsDtoV2? habitDto = await dbContext.Habits
            .Where(h => h.Id == id && h.UserId == userId)
            .Select(HabitQueries.ProjectToDtoWithTagsV2()).FirstOrDefaultAsync(cancellationToken);

        if (habitDto == null)
        {
            return NotFound();
        }

        ExpandoObject shapedHabitDto = dataShapingService.ShapeData(habitDto, query.Fields);

        if (query.IncludeLinks)
        {
            List<LinkDto> links = CreateLinksForHabit(id, query.Fields);

            shapedHabitDto.TryAdd("links", links);
        }

        return Ok(shapedHabitDto);
    }

    /// <summary>
    /// Creates a new habit
    /// </summary>
    /// <param name="createHabitDto">The habit creation details</param>
    /// <param name="acceptHeader">Controls HATEOAS link generation</param>
    /// <param name="validator">Validator for the creation request</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The created habit</returns>
    [HttpPost]
    [ProducesResponseType<HabitDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<HabitDto>> CreateHabit(CreateHabitDto createHabitDto,
        [FromHeader] AcceptHeaderDto acceptHeader,
        IValidator<CreateHabitDto> validator,
        CancellationToken cancellationToken)
    {
        string? userId = await userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }
        
        await validator.ValidateAndThrowAsync(createHabitDto, cancellationToken);

        Habit habit = createHabitDto.ToEntity(userId);

        if (habit.AutomationSource is not null &&
            await dbContext.Habits.AnyAsync(h => h.UserId == userId && h.AutomationSource == habit.AutomationSource,
                cancellationToken: cancellationToken))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"Only one habit with this automation source is allowed: '{habit.AutomationSource}'");
        }

        dbContext.Habits.Add(habit);

        await dbContext.SaveChangesAsync(cancellationToken);

        HabitDto habitDto = habit.ToDto();

        if (acceptHeader.IncludeLinks)
        {
            habitDto.Links = CreateLinksForHabit(habitDto.Id, null);
        }

        return CreatedAtAction(nameof(GetHabit), new { id = habit.Id }, habitDto);
    }

    /// <summary>
    /// Updates an existing habit
    /// </summary>
    /// <param name="id">The habit ID</param>
    /// <param name="updateHabitDto">The habit update details</param>
    /// <param name="validator"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>No content on success</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateHabit(string id, UpdateHabitDto updateHabitDto,
        IValidator<UpdateHabitDto> validator,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(updateHabitDto, cancellationToken);
        
        string? userId = await userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        Habit? habit =
            await dbContext.Habits.FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId, cancellationToken);

        if (habit == null)
        {
            return NotFound();
        }

        if (habit.AutomationSource is null &&
            updateHabitDto.AutomationSource is not null &&
            await dbContext.Habits.AnyAsync(
                h => h.UserId == userId && h.AutomationSource == updateHabitDto.AutomationSource,
                cancellationToken: cancellationToken))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"Only one habit with this automation source is allowed: '{habit.AutomationSource}'");
        }

        habit.UpdateFromDto(updateHabitDto);

        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Partially updates an existing habit using JSON Patch
    /// </summary>
    /// <param name="id">The habit ID</param>
    /// <param name="patchDocument">The JSON Patch document</param>
    /// <param name="cancellationToken"></param>
    /// <returns>No content on success</returns>
    [HttpPatch("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> PatchHabit(string id, JsonPatchDocument<HabitDto> patchDocument,
        CancellationToken cancellationToken)
    {
        string? userId = await userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        Habit? habit =
            await dbContext.Habits.FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId, cancellationToken);

        if (habit == null)
        {
            return NotFound();
        }

        HabitDto habitDto = habit.ToDto();

        patchDocument.ApplyTo(habitDto, ModelState);

        if (!TryValidateModel(habitDto))
        {
            return ValidationProblem(ModelState);
        }

        habit.Name = habitDto.Name;
        habit.Description = habitDto.Description;
        habit.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Deletes a habit
    /// </summary>
    /// <param name="id">The habit ID</param>
    /// <param name="cancellationToken"></param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteHabit(string id, CancellationToken cancellationToken)
    {
        string? userId = await userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        Habit? habit =
            await dbContext.Habits.FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId, cancellationToken);

        if (habit == null)
        {
            return NotFound();
        }

        dbContext.Habits.Remove(habit);

        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private List<LinkDto> CreateLinksForHabits(
        HabitsQueryParameters parameters,
        bool hasNextPage,
        bool hasPreviousPage)
    {
        List<LinkDto> links =
        [
            linkService.Create(nameof(GetHabits), "self", HttpMethods.Get, new
            {
                page = parameters.Page,
                pageSize = parameters.PageSize,
                fields = parameters.Fields,
                q = parameters.Search,
                sort = parameters.Sort,
                type = parameters.Type,
                status = parameters.Status,
            }),
            linkService.Create(nameof(CreateHabit), "create", HttpMethods.Post)
        ];

        if (hasNextPage)
        {
            links.Add(linkService.Create(nameof(GetHabits), "next-page", HttpMethods.Get, new
            {
                page = parameters.Page + 1,
                pageSize = parameters.PageSize,
                fields = parameters.Fields,
                q = parameters.Search,
                sort = parameters.Sort,
                type = parameters.Type,
                status = parameters.Status,
            }));
        }

        if (hasPreviousPage)
        {
            links.Add(linkService.Create(nameof(GetHabits), "previous-page", HttpMethods.Get, new
            {
                page = parameters.Page - 1,
                pageSize = parameters.PageSize,
                fields = parameters.Fields,
                q = parameters.Search,
                sort = parameters.Sort,
                type = parameters.Type,
                status = parameters.Status,
            }));
        }

        return links;
    }

    private List<LinkDto> CreateLinksForHabit(string id, string? fields)
    {
        List<LinkDto> links =
        [
            linkService.Create(nameof(GetHabit), "self", HttpMethods.Get, new { id, fields }),
            linkService.Create(nameof(UpdateHabit), "update", HttpMethods.Put, new { id }),
            linkService.Create(nameof(PatchHabit), "partial-update", HttpMethods.Patch, new { id }),
            linkService.Create(nameof(DeleteHabit), "delete", HttpMethods.Delete, new { id }),
            linkService.Create(
                nameof(HabitTagsController.UpsertHabitTags),
                "upsert-tags",
                HttpMethods.Put,
                new { habitId = id },
                HabitTagsController.Name),
        ];
        return links;
    }
}
