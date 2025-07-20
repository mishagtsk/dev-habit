using System.Dynamic;
using System.Net.Mime;
using DevHabit.Api.Controllers;
using DevHabit.Api.Database;
using DevHabit.Api.DTOs.Common;
using DevHabit.Api.DTOs.Habits;
using DevHabit.Api.Entities;
using DevHabit.Api.Services;
using DevHabit.Api.Services.Sorting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;

namespace DevHabit.Api.Endpoints;

public static class HabitEndpoints
{
    public static IEndpointRouteBuilder MapHabitEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("habits")
            .WithTags("Habits")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Member))
            .WithOpenApi()
            .WithMetadata(new ProducesAttribute(
                MediaTypeNames.Application.Json,
                CustomMediaTypeNames.Application.JsonV1,
                CustomMediaTypeNames.Application.JsonV2,
                CustomMediaTypeNames.Application.HateoasJson,
                CustomMediaTypeNames.Application.HateoasJsonV1,
                CustomMediaTypeNames.Application.HateoasJsonV2));
        
        group.MapGet("/", GetHabits)
            .WithName(nameof(GetHabits))
            .Produces<PaginationResult<HabitDto>>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .WithOpenApi();
        
        group.MapGet("/v1/{id}", GetHabit)
            .WithName(nameof(GetHabit))
            .Produces<HabitWithTagsDto>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .WithOpenApi();
        
        group.MapGet("/v2/{id}", GetHabitV2)
            .WithName(nameof(GetHabitV2))
            .Produces<HabitWithTagsDtoV2>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .WithOpenApi();
        
        return app;
    }
    
    /// <summary>
    /// Retrieves a paginated list of habits
    /// </summary>
    /// <returns>Paginated list of habits</returns>
    private static async Task<IResult> GetHabits(
        HttpContext context,
        UserContext userContext,
        ApplicationDbContext dbContext,
        LinkService linkService,
        SortMappingProvider sortMappingProvider, DataShapingService dataShapingService,
        CancellationToken cancellationToken)
    {
        string? userId = await userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        // Extract query parameters. Alternative - pass each as a param
        var query = new HabitsQueryParameters
        {
            Page = context.Request.Query.TryGetValue("page", out StringValues page)
                   && int.TryParse(page, out int p)
                ? p
                : 1,
            PageSize = context.Request.Query.TryGetValue("pageSize", out StringValues pageSize)
                       && int.TryParse(pageSize, out int ps)
                ? ps
                : 10,
            Fields = context.Request.Query["fields"].ToString(),
            Search = context.Request.Query["q"].ToString(),
            Sort = context.Request.Query["sort"].ToString(),
            Type = context.Request.Query.TryGetValue("type", out StringValues type)
                   && Enum.TryParse(type, out HabitType t)
                ? t
                : null,
            Status = context.Request.Query.TryGetValue("status", out StringValues status)
                     && Enum.TryParse(status, out HabitStatus s)
                ? s
                : null,
            Accept = context.Request.Headers.Accept.ToString()
        };
        
        if (!sortMappingProvider.ValidateMappings<HabitDto, Habit>(query.Sort))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provided sort parameter is not valid: {query.Sort}.");
        }

        if (!dataShapingService.Validate<HabitDto>(query.Fields))
        {
            return Results.Problem(
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
                query.IncludeLinks ? h => CreateLinksForHabit(linkService, h.Id, query.Fields) : null),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };

        if (query.IncludeLinks)
        {
            paginationResult.Links =
                CreateLinksForHabits(linkService, query, paginationResult.HasNextPage, paginationResult.HasPreviousPage);
        }
        
        return TypedResults.Ok(paginationResult);
    }

    /// <summary>
    /// Retrieves a specific habit by ID
    /// </summary>
    /// <returns>The requested habit</returns>
    public static async Task<IResult> GetHabit(string id,
        HttpContext context,
        UserContext userContext,
        ApplicationDbContext dbContext,
        LinkService linkService,
        DataShapingService dataShapingService,
        CancellationToken cancellationToken)
    {
        string? userId = await userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }
        
        var query = new HabitQueryParameters
        {
            Fields = context.Request.Query["fields"].ToString(),
            Accept = context.Request.Headers.Accept.ToString()
        };
        
        if (!dataShapingService.Validate<HabitWithTagsDto>(query.Fields))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provided data shaping fields are not valid: {query.Fields}.");
        }

        HabitWithTagsDto? habitDto = await dbContext.Habits
            .Where(h => h.Id == id && h.UserId == userId)
            .Select(HabitQueries.ProjectToDtoWithTags()).FirstOrDefaultAsync(cancellationToken);

        if (habitDto == null)
        {
            return Results.NotFound();
        }

        ExpandoObject shapedHabitDto = dataShapingService.ShapeData(habitDto, query.Fields);

        if (query.IncludeLinks)
        {
            List<LinkDto> links = CreateLinksForHabit(linkService, id, query.Fields);

            shapedHabitDto.TryAdd("links", links);
        }

        return TypedResults.Ok(shapedHabitDto);
    }

    /// <summary>
    /// Retrieves a specific habit by ID with version 2 of the API
    /// </summary>
    /// <returns>The requested habit</returns>
    public static async Task<IResult> GetHabitV2(string id,
        HttpContext context,
        UserContext userContext,
        ApplicationDbContext dbContext,
        LinkService linkService,
        DataShapingService dataShapingService,
        CancellationToken cancellationToken)
    {
        string? userId = await userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }
        
        var query = new HabitQueryParameters
        {
            Fields = context.Request.Query["fields"].ToString(),
            Accept = context.Request.Headers.Accept.ToString()
        };
        
        if (!dataShapingService.Validate<HabitWithTagsDtoV2>(query.Fields))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provided data shaping fields are not valid: {query.Fields}.");
        }

        HabitWithTagsDtoV2? habitDto = await dbContext.Habits
            .Where(h => h.Id == id && h.UserId == userId)
            .Select(HabitQueries.ProjectToDtoWithTagsV2()).FirstOrDefaultAsync(cancellationToken);

        if (habitDto == null)
        {
            return Results.NotFound();
        }

        ExpandoObject shapedHabitDto = dataShapingService.ShapeData(habitDto, query.Fields);

        if (query.IncludeLinks)
        {
            List<LinkDto> links = CreateLinksForHabit(linkService, id, query.Fields);

            shapedHabitDto.TryAdd("links", links);
        }

        return TypedResults.Ok(shapedHabitDto);
    }
    
    private static List<LinkDto> CreateLinksForHabits(LinkService linkService,
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
            linkService.Create(nameof(HabitsController.CreateHabit), "create", HttpMethods.Post)
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

    private static List<LinkDto> CreateLinksForHabit(LinkService linkService, string id, string? fields)
    {
        List<LinkDto> links =
        [
            linkService.Create(nameof(HabitsController.GetHabit), "self", HttpMethods.Get, new { id, fields }),
            linkService.Create(nameof(HabitsController.UpdateHabit), "update", HttpMethods.Put, new { id }),
            linkService.Create(nameof(HabitsController.PatchHabit), "partial-update", HttpMethods.Patch, new { id }),
            linkService.Create(nameof(HabitsController.DeleteHabit), "delete", HttpMethods.Delete, new { id }),
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
