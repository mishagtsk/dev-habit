using DevHabit.Api.Database;
using DevHabit.Api.DTOs.Common;
using DevHabit.Api.DTOs.Habits;
using DevHabit.Api.Entities;
using DevHabit.Api.Services.Sorting;
using FluentValidation;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;

[ApiController] 
[Route("[controller]")]
public sealed class HabitsController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginationResult<HabitDto>>> GetHabits([FromQuery] HabitQueryParameters query, 
        SortMappingProvider sortMappingProvider)
    {
        query.Search = query.Search?.Trim().ToLower();

        if (!sortMappingProvider.ValidateMappings<HabitDto, Habit>(query.Sort))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provided sort parameter is not valid: {query.Sort}.");
        }
        SortMapping[] sortMappings = sortMappingProvider.GetMappings<HabitDto, Habit>();

        IQueryable<HabitDto> habitsQuery = dbContext.Habits
            .Where(h => query.Search == null || 
                        h.Name.ToLower().Contains(query.Search) ||
                        h.Description != null && h.Description.ToLower().Contains(query.Search))
            .Where(h => query.Type == null || query.Type.Value == h.Type)
            .Where(h => query.Status == null || query.Status.Value == h.Status)
            .ApplySort(query.Sort, sortMappings)
            .Select(HabitQueries.ProjectToDto());

        return Ok(await PaginationResult<HabitDto>.CreateAsync(habitsQuery, query.Page, query.PageSize));
    }


    [HttpGet("{id}")]
    public async Task<ActionResult<HabitWithTagsDto>> GetHabit(string id)
    {
        HabitWithTagsDto? habitDto = await dbContext.Habits
            .Where(h => h.Id == id)
            .Select(HabitQueries.ProjectToDtoWithTags()).FirstOrDefaultAsync();

        if (habitDto == null)
        {
            return NotFound();
        }
        
        return Ok(habitDto);
    }

    [HttpPost]
    public async Task<ActionResult<HabitDto>> CreateHabitAsync(CreateHabitDto createHabitDto,
        IValidator<CreateHabitDto> validator)
    {
        await validator.ValidateAndThrowAsync(createHabitDto);

        Habit habit = createHabitDto.ToEntity();

        dbContext.Habits.Add(habit);

        await dbContext.SaveChangesAsync();

        HabitDto habitDto = habit.ToDto();

        return CreatedAtAction(nameof(GetHabit), new { id = habit.Id }, habitDto);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateHabit(string id, UpdateHabitDto updateHabitDto)
    {
        Habit? habit = await dbContext.Habits.FirstOrDefaultAsync(h => h.Id == id);

        if (habit == null)
        {
            return NotFound();
        }
        
        habit.UpdateFromDto(updateHabitDto);
        
        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult> PatchHabit(string id, JsonPatchDocument<HabitDto> patchDocument)
    {
        Habit? habit = await dbContext.Habits.FirstOrDefaultAsync(h => h.Id == id);

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
        
        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteHabit(string id)
    {
        Habit? habit = await dbContext.Habits.FirstOrDefaultAsync(h => h.Id == id);

        if (habit == null)
        {
            return NotFound();
        }
        
        dbContext.Habits.Remove(habit);
        
        await dbContext.SaveChangesAsync();

        return NoContent();
    }
}
