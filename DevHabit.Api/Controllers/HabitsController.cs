using System.Linq.Expressions;
using DevHabit.Api.Database;
using DevHabit.Api.DTOs.Habits;
using DevHabit.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;

[ApiController] 
[Route("[controller]")]
public sealed class HabitsController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<HabitCollectionDto>> GetHabits()
    {
        List<HabitDto> habits = await dbContext.Habits.Select(HabitQueries.ProjectToDto()).ToListAsync();

        var habitCollectionDto = new HabitCollectionDto { Data = habits };
        
        return Ok(habitCollectionDto);
    }


    [HttpGet("{id}")]
    public async Task<ActionResult<HabitDto>> GetHabit(string id)
    {
        HabitDto? habitDto = await dbContext.Habits
            .Where(h => h.Id == id)
            .Select(HabitQueries.ProjectToDto()).FirstOrDefaultAsync();

        if (habitDto == null)
        {
            return NotFound();
        }
        
        return Ok(habitDto);
    }

    [HttpPost]
    public async Task<ActionResult<HabitDto>> CreateHabitAsync(CreateHabitDto createHabitDto)
    {
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
}
