using AnswerService.Application.DTOs;
using FluentValidation;

namespace AnswerService.Api.Validators;

/// <summary>
/// Validators for Answer-related DTOs using FluentValidation.
/// </summary>

public class SubmitAnswerRequestValidator : AbstractValidator<SubmitAnswerRequest>
{
    public SubmitAnswerRequestValidator()
    {
        RuleFor(x => x.AssessmentId)
            .NotEmpty()
            .WithMessage("Assessment ID is required");

        RuleFor(x => x.CandidateId)
            .NotEmpty()
            .WithMessage("Candidate ID is required");

        RuleFor(x => x.QuestionId)
            .NotEmpty()
            .WithMessage("Question ID is required");

        RuleFor(x => x)
            .Must(x => x.SelectedOptionId.HasValue || !string.IsNullOrWhiteSpace(x.AnswerText))
            .WithMessage("Either a selected option or answer text is required");
    }
}

public class UpdateAnswerRequestValidator : AbstractValidator<AnswerSaveRequest>
{
    public UpdateAnswerRequestValidator()
    {
        RuleFor(x => x.QuestionId)
            .NotEmpty()
            .WithMessage("Question ID is required");

        RuleFor(x => x)
            .Must(x =>
                x.SelectedOptionId.HasValue ||
                !string.IsNullOrWhiteSpace(x.DescriptiveAnswer) ||
                !string.IsNullOrWhiteSpace(x.CodingAnswer))
            .WithMessage("Either a selected option or an answer body is required");
    }
}
