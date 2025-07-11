using FluentValidation;

namespace DevHabit.Api.DTOs.Entries.Validation;

public sealed class CreateEntryDtoValidator : AbstractValidator<CreateEntryDto>
{
    public CreateEntryDtoValidator()
    {
        RuleFor(x => x.HabitId)
            .NotEmpty()
            .WithMessage("Habit does not exist.");

        RuleFor(x => x.Value)
            .GreaterThan(0)
            .WithMessage("Value must be greater than or equal to 0.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .When(x => x.Notes is not null);

        RuleFor(x => x.Date)
            .NotEmpty()
            .Must(date => date <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date cannot be in the future.");
    }
}
