using CandidateService.Application.Events;
using MassTransit;
using ResultService.Application.Services;
using ResultService.Domain.Entities;

namespace ResultService.Api.Events;

public class CandidateAssessmentAssignedConsumer : IConsumer<CandidateAssessmentAssignedEvent>
{
    private readonly IResultService _resultService;
    private readonly ILogger<CandidateAssessmentAssignedConsumer> _logger;

    public CandidateAssessmentAssignedConsumer(
        IResultService resultService,
        ILogger<CandidateAssessmentAssignedConsumer> logger)
    {
        _resultService = resultService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CandidateAssessmentAssignedEvent> context)
    {
        var message = context.Message;

        var existing = await _resultService.GetByCandidateAndAssessmentAsync(message.CandidateId, message.AssessmentId);
        if (existing != null)
        {
            _logger.LogInformation(
                "Skipping CandidateAssessmentAssignedEvent because result already exists for CandidateId={CandidateId}, AssessmentId={AssessmentId}",
                message.CandidateId,
                message.AssessmentId);
            return;
        }

        var result = new Result
        {
            CandidateId = message.CandidateId,
            AssessmentId = message.AssessmentId,
            Score = 0,
            MaxScore = 0,
            Percentage = 0,
            Status = "Assigned",
            TotalQuestions = 0,
            CorrectAnswers = 0,
            WrongAnswers = 0,
            SkippedQuestions = 0,
            StartedAt = message.AssignedAt,
            CompletedAt = message.AssignedAt,
            EvaluatedAt = message.AssignedAt,
            CalculatedAt = null,
            PublishedAt = null,
            Remarks = "Assessment assigned",
            IsPassed = false,
            PassingPercentage = null
        };

        await _resultService.CreateAsync(result);

        _logger.LogInformation(
            "Created initial assigned result for CandidateId={CandidateId}, AssessmentId={AssessmentId}",
            message.CandidateId,
            message.AssessmentId);
    }
}
