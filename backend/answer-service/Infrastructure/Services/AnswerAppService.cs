using AnswerService.Application.Events;
using AnswerService.Application.Services;
using AnswerService.Domain.Entities;
using AnswerService.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace AnswerService.Infrastructure.Services;

public class AnswerAppService : IAnswerService
{
    private readonly AnswerDbContext _dbContext;
    private readonly IPublishEndpoint _publishEndpoint;

    public AnswerAppService(AnswerDbContext dbContext, IPublishEndpoint publishEndpoint)
    {
        _dbContext = dbContext;
        _publishEndpoint = publishEndpoint;
    }

    // Query methods
    public async Task<IReadOnlyList<Answer>> GetAllAsync()
        => await _dbContext.Answers.AsNoTracking().ToListAsync();

    public async Task<Answer?> GetByIdAsync(Guid id)
        => await _dbContext.Answers.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);

    public async Task<IReadOnlyList<Answer>> GetByAssessmentIdAsync(Guid assessmentId)
        => await _dbContext.Answers
            .AsNoTracking()
            .Where(a => a.AssessmentId == assessmentId)
            .ToListAsync();

    public async Task<IReadOnlyList<Answer>> GetByCandidateIdAsync(Guid candidateId)
        => await _dbContext.Answers
            .AsNoTracking()
            .Where(a => a.CandidateId == candidateId)
            .ToListAsync();

    public async Task<IReadOnlyList<Answer>> GetByCandidateAndAssessmentAsync(Guid candidateId, Guid assessmentId)
        => await _dbContext.Answers
            .AsNoTracking()
            .Where(a => a.CandidateId == candidateId && a.AssessmentId == assessmentId)
            .ToListAsync();

    // Command methods
    public async Task<Answer> CreateAsync(Answer answer)
    {
        answer.Id = Guid.NewGuid();
        answer.SubmittedAt = DateTime.UtcNow;

        _dbContext.Answers.Add(answer);
        await _dbContext.SaveChangesAsync();

        await _publishEndpoint.Publish(new AnswerCreatedEvent
        {
            AnswerId = answer.Id,
            AssessmentId = answer.AssessmentId,
            CandidateId = answer.CandidateId,
            SubmittedAt = answer.SubmittedAt
        });

        return answer;
    }

    public async Task<Answer> SubmitAnswerAsync(Guid assessmentId, Guid candidateId, Guid questionId, Guid? selectedOptionId, string answerText)
    {
        if (assessmentId == Guid.Empty)
            throw new ArgumentException("Assessment ID is required", nameof(assessmentId));

        if (candidateId == Guid.Empty)
            throw new ArgumentException("Candidate ID is required", nameof(candidateId));

        if (questionId == Guid.Empty)
            throw new ArgumentException("Question ID is required", nameof(questionId));

        if (selectedOptionId == null && string.IsNullOrWhiteSpace(answerText))
            throw new ArgumentException("Either answer text or selected option must be provided");

        var normalizedAnswerText = answerText?.Trim() ?? string.Empty;
        var answer = await _dbContext.Answers
            .FirstOrDefaultAsync(a =>
                a.AssessmentId == assessmentId &&
                a.CandidateId == candidateId &&
                a.QuestionId == questionId);

        if (answer == null)
        {
            answer = new Answer
            {
                Id = Guid.NewGuid(),
                AssessmentId = assessmentId,
                CandidateId = candidateId,
                QuestionId = questionId
            };
            _dbContext.Answers.Add(answer);
        }

        answer.SelectedOptionId = selectedOptionId;
        answer.AnswerText = normalizedAnswerText;
        answer.SubmittedAt = DateTime.UtcNow;
        answer.IsCorrect = null;
        answer.PointsObtained = null;
        answer.GradedAt = null;
        answer.GradingNotes = string.Empty;

        await _dbContext.SaveChangesAsync();

        await _publishEndpoint.Publish(new AnswerCreatedEvent
        {
            AnswerId = answer.Id,
            AssessmentId = answer.AssessmentId,
            CandidateId = answer.CandidateId,
            SubmittedAt = answer.SubmittedAt
        });

        return answer;
    }

    public async Task<Answer> UpdateAsync(Answer answer)
    {
        _dbContext.Answers.Update(answer);
        await _dbContext.SaveChangesAsync();
        return answer;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var answer = await _dbContext.Answers.FindAsync(id);
        if (answer == null)
            return false;

        _dbContext.Answers.Remove(answer);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<Answer> GradeAnswerAsync(Guid answerId, bool isCorrect, int pointsObtained, string? notes = null)
    {
        var answer = await _dbContext.Answers.FindAsync(answerId)
            ?? throw new InvalidOperationException($"Answer with ID {answerId} not found");

        answer.IsCorrect = isCorrect;
        answer.PointsObtained = pointsObtained;
        answer.GradedAt = DateTime.UtcNow;
        if (notes != null)
            answer.GradingNotes = notes;

        _dbContext.Answers.Update(answer);
        await _dbContext.SaveChangesAsync();

        return answer;
    }
}
