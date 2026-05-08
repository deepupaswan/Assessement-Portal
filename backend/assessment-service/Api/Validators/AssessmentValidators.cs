using AssessmentService.Api.Models;
using FluentValidation;

namespace AssessmentService.Api.Validators;

/// <summary>
/// Validators for Assessment-related DTOs using FluentValidation.
/// </summary>

public class CreateAssessmentRequestValidator : AbstractValidator<CreateAssessmentRequest>
{
    public CreateAssessmentRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required")
            .MinimumLength(3)
            .WithMessage("Title must be at least 3 characters long")
            .MaximumLength(255)
            .WithMessage("Title cannot exceed 255 characters")
            .Matches(@"^[a-zA-Z0-9\s\-.,()&':;/]+$")
            .WithMessage("Title contains invalid characters");

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .WithMessage("Description cannot exceed 2000 characters")
            .Must(d => string.IsNullOrEmpty(d) || !d.Contains("<script>"))
            .WithMessage("Description contains invalid content");

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0)
            .WithMessage("Duration must be greater than 0")
            .LessThanOrEqualTo(480)
            .WithMessage("Duration cannot exceed 8 hours (480 minutes)");
    }
}

public class UpdateAssessmentRequestValidator : AbstractValidator<UpdateAssessmentRequest>
{
    public UpdateAssessmentRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required")
            .MinimumLength(3)
            .WithMessage("Title must be at least 3 characters long")
            .MaximumLength(255)
            .WithMessage("Title cannot exceed 255 characters");

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .WithMessage("Description cannot exceed 2000 characters");

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0)
            .WithMessage("Duration must be greater than 0")
            .LessThanOrEqualTo(480)
            .WithMessage("Duration cannot exceed 8 hours (480 minutes)");
    }
}
