using FluentValidation;

namespace DevHabit.Api.DTOs.Tags.Validation;

public sealed class CreateTagDtoValidator : AbstractValidator<CreateTagDto>
{
    public CreateTagDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(3).MaximumLength(50);
        
        RuleFor(x => x.Description).MaximumLength(50);
    }
}
