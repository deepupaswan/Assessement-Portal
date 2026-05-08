using AnswerService.Application.Repositories;
using AnswerService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AnswerService.Infrastructure.Persistence.Repositories;

public class AnswerRepository : IAnswerRepository
{
    private readonly AnswerDbContext _dbContext;

    public AnswerRepository(AnswerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Answer>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _dbContext.Answers.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<Answer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbContext.Answers.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Answer>> GetByAssessmentIdAsync(Guid assessmentId, CancellationToken cancellationToken = default)
        => await _dbContext.Answers
            .AsNoTracking()
            .Where(a => a.AssessmentId == assessmentId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Answer>> GetByCandidateIdAsync(Guid candidateId, CancellationToken cancellationToken = default)
        => await _dbContext.Answers
            .AsNoTracking()
            .Where(a => a.CandidateId == candidateId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Answer>> GetByCandidateAndAssessmentAsync(Guid candidateId, Guid assessmentId, CancellationToken cancellationToken = default)
        => await _dbContext.Answers
            .AsNoTracking()
            .Where(a => a.CandidateId == candidateId && a.AssessmentId == assessmentId)
            .ToListAsync(cancellationToken);

    public async Task<Answer?> GetByAssessmentCandidateAndQuestionAsync(Guid assessmentId, Guid candidateId, Guid questionId, CancellationToken cancellationToken = default)
        => await _dbContext.Answers
            .FirstOrDefaultAsync(
                a => a.AssessmentId == assessmentId && a.CandidateId == candidateId && a.QuestionId == questionId,
                cancellationToken);

    public async Task AddAsync(Answer answer, CancellationToken cancellationToken = default)
        => await _dbContext.Answers.AddAsync(answer, cancellationToken);

    public void Update(Answer answer) => _dbContext.Answers.Update(answer);

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var answer = await _dbContext.Answers.FindAsync(new object[] { id }, cancellationToken);
        if (answer == null)
        {
            return false;
        }

        _dbContext.Answers.Remove(answer);
        return true;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
