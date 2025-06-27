using DevHabit.Api.DTOs.Habits;
using DevHabit.Api.DTOs.Habits.Validation;
using DevHabit.Api.Entities;
using FluentValidation.TestHelper;

namespace DevHabit.UnitTests.Validators;

public sealed class CreateHabitDtoValidatorTests
{
    private readonly CreateHabitDtoValidator _validator = new();

    [Fact]
    public async Task Validate_ShouldNotReturnError_WhenAllPropertiesAreValid()
    {
        // Arrange
        var dto = new CreateHabitDto
        {
            Name = "Read Books",
            Description = "Read technical books to improve skills",
            Type = HabitType.Measurable,
            Frequency = new FrequencyDto
            {
                Type = FrequencyType.Daily,
                TimesPerPeriod = 1
            },
            Target = new TargetDto
            {
                Value = 30,
                Unit = "pages"
            }
        };

        // Act
        TestValidationResult<CreateHabitDto>? result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_ShouldReturnError_WhenNameIsEmpty()
    {
        // Arrange
        var dto = new CreateHabitDto
        {
            Name = string.Empty,
            Type = HabitType.Measurable,
            Frequency = new FrequencyDto
            {
                Type = FrequencyType.Daily,
                TimesPerPeriod = 1
            },
            Target = new TargetDto
            {
                Value = 30,
                Unit = "pages"
            }
        };

        // Act
        TestValidationResult<CreateHabitDto>? result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Validate_ShouldReturnError_WhenNameIsTooShort()
    {
        // Arrange
        var dto = new CreateHabitDto
        {
            Name = "ab",
            Type = HabitType.Measurable,
            Frequency = new FrequencyDto
            {
                Type = FrequencyType.Daily,
                TimesPerPeriod = 1
            },
            Target = new TargetDto
            {
                Value = 30,
                Unit = "pages"
            }
        };

        // Act
        TestValidationResult<CreateHabitDto>? result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Validate_ShouldReturnError_WhenNameExceedsMaxLength()
    {
        // Arrange
        var dto = new CreateHabitDto
        {
            Name = new string('a', 101),
            Type = HabitType.Measurable,
            Frequency = new FrequencyDto
            {
                Type = FrequencyType.Daily,
                TimesPerPeriod = 1
            },
            Target = new TargetDto
            {
                Value = 30,
                Unit = "pages"
            }
        };

        // Act
        TestValidationResult<CreateHabitDto>? result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Validate_ShouldReturnError_WhenDescriptionExceedsMaxLength()
    {
        // Arrange
        var dto = new CreateHabitDto
        {
            Name = "Read Books",
            Description = new string('a', 501),
            Type = HabitType.Measurable,
            Frequency = new FrequencyDto
            {
                Type = FrequencyType.Daily,
                TimesPerPeriod = 1
            },
            Target = new TargetDto
            {
                Value = 30,
                Unit = "pages"
            }
        };

        // Act
        TestValidationResult<CreateHabitDto>? result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public async Task Validate_ShouldReturnError_WhenTypeIsInvalid()
    {
        // Arrange
        var dto = new CreateHabitDto
        {
            Name = "Read Books",
            Type = (HabitType)999,
            Frequency = new FrequencyDto
            {
                Type = FrequencyType.Daily,
                TimesPerPeriod = 1
            },
            Target = new TargetDto
            {
                Value = 30,
                Unit = "pages"
            }
        };

        // Act
        TestValidationResult<CreateHabitDto>? result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Type);
    }

    [Fact]
    public async Task Validate_ShouldReturnError_WhenFrequencyTypeIsInvalid()
    {
        // Arrange
        var dto = new CreateHabitDto
        {
            Name = "Read Books",
            Type = HabitType.Measurable,
            Frequency = new FrequencyDto
            {
                Type = (FrequencyType)999,
                TimesPerPeriod = 1
            },
            Target = new TargetDto
            {
                Value = 30,
                Unit = "pages"
            }
        };

        // Act
        TestValidationResult<CreateHabitDto>? result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Frequency.Type);
    }

    [Fact]
    public async Task Validate_ShouldReturnError_WhenFrequencyTimesPerPeriodIsZero()
    {
        // Arrange
        var dto = new CreateHabitDto
        {
            Name = "Read Books",
            Type = HabitType.Measurable,
            Frequency = new FrequencyDto
            {
                Type = FrequencyType.Daily,
                TimesPerPeriod = 0
            },
            Target = new TargetDto
            {
                Value = 30,
                Unit = "pages"
            }
        };

        // Act
        TestValidationResult<CreateHabitDto>? result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Frequency.TimesPerPeriod);
    }

    [Fact]
    public async Task Validate_ShouldReturnError_WhenTargetValueIsZero()
    {
        // Arrange
        var dto = new CreateHabitDto
        {
            Name = "Read Books",
            Type = HabitType.Measurable,
            Frequency = new FrequencyDto
            {
                Type = FrequencyType.Daily,
                TimesPerPeriod = 1
            },
            Target = new TargetDto
            {
                Value = 0,
                Unit = "pages"
            }
        };

        // Act
        TestValidationResult<CreateHabitDto>? result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Target.Value);
    }

    [Fact]
    public async Task Validate_ShouldReturnError_WhenTargetUnitIsEmpty()
    {
        // Arrange
        var dto = new CreateHabitDto
        {
            Name = "Read Books",
            Type = HabitType.Measurable,
            Frequency = new FrequencyDto
            {
                Type = FrequencyType.Daily,
                TimesPerPeriod = 1
            },
            Target = new TargetDto
            {
                Value = 30,
                Unit = string.Empty
            }
        };

        // Act
        TestValidationResult<CreateHabitDto>? result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Target.Unit);
    }

    [Fact]
    public async Task Validate_ShouldReturnError_WhenTargetUnitIsInvalid()
    {
        // Arrange
        var dto = new CreateHabitDto
        {
            Name = "Read Books",
            Type = HabitType.Measurable,
            Frequency = new FrequencyDto
            {
                Type = FrequencyType.Daily,
                TimesPerPeriod = 1
            },
            Target = new TargetDto
            {
                Value = 30,
                Unit = "invalid-unit"
            }
        };

        // Act
        TestValidationResult<CreateHabitDto>? result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Target.Unit);
    }

    [Fact]
    public async Task Validate_ShouldReturnError_WhenBinaryHabitHasInvalidUnit()
    {
        // Arrange
        var dto = new CreateHabitDto
        {
            Name = "Read Books",
            Type = HabitType.Binary,
            Frequency = new FrequencyDto
            {
                Type = FrequencyType.Daily,
                TimesPerPeriod = 1
            },
            Target = new TargetDto
            {
                Value = 1,
                Unit = "pages" // Binary habits should only use sessions or tasks
            }
        };

        // Act
        TestValidationResult<CreateHabitDto>? result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Target.Unit);
    }

    [Fact]
    public async Task Validate_ShouldReturnError_WhenEndDateIsInPast()
    {
        // Arrange
        var dto = new CreateHabitDto
        {
            Name = "Read Books",
            Type = HabitType.Measurable,
            Frequency = new FrequencyDto
            {
                Type = FrequencyType.Daily,
                TimesPerPeriod = 1
            },
            Target = new TargetDto
            {
                Value = 30,
                Unit = "pages"
            },
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))
        };

        // Act
        TestValidationResult<CreateHabitDto>? result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EndDate);
    }

    [Fact]
    public async Task Validate_ShouldReturnError_WhenMilestoneTargetIsZero()
    {
        // Arrange
        var dto = new CreateHabitDto
        {
            Name = "Read Books",
            Type = HabitType.Measurable,
            Frequency = new FrequencyDto
            {
                Type = FrequencyType.Daily,
                TimesPerPeriod = 1
            },
            Target = new TargetDto
            {
                Value = 30,
                Unit = "pages"
            },
            Milestone = new MilestoneDto
            {
                Target = 0,
                Current = 0
            }
        };

        // Act
        TestValidationResult<CreateHabitDto>? result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Milestone!.Target);
    }
}
