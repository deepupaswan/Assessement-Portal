using ResultService.Application.DTOs;
using FluentValidation;

namespace ResultService.Api.Validators;

/// <summary>
/// Validators for Result-related DTOs using FluentValidation.
/// </summary>

public class CreateResultRequestValidator : AbstractValidator<CreateResultRequest>
{
    public CreateResultRequestValidator()
    {
        RuleFor(x => x.CandidateAssessmentId)
            .NotEmpty()
            .WithMessage("Candidate Assessment ID is required");

        RuleFor(x => x.TotalQuestions)
            .GreaterThan(0)
            .WithMessage("Total questions must be greater than 0");

        RuleFor(x => x.CorrectAnswers)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Correct answers cannot be negative")
            .LessThanOrEqualTo(x => x.TotalQuestions)
            .WithMessage("Correct answers cannot exceed total questions");

        RuleFor(x => x.ScorePercentage)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Score percentage must be between 0 and 100")
            .LessThanOrEqualTo(100)
            .WithMessage("Score percentage must be between 0 and 100");
    }
}

public class UpdateResultRequestValidator : AbstractValidator<UpdateResultRequest>
{
    public UpdateResultRequestValidator()
    {
        RuleFor(x => x.TotalQuestions)
            .GreaterThan(0)
            .WithMessage("Total questions must be greater than 0");

        RuleFor(x => x.CorrectAnswers)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Correct answers cannot be negative")
            .LessThanOrEqualTo(x => x.TotalQuestions)
            .WithMessage("Correct answers cannot exceed total questions");

        RuleFor(x => x.ScorePercentage)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Score percentage must be between 0 and 100")
            .LessThanOrEqualTo(100)
            .WithMessage("Score percentage must be between 0 and 100");
    }
}
