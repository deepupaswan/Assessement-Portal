using AnswerService.Domain.Entities;

namespace AnswerService.Application.Repositories;

public interface IAnswerRepository
{
    Task<IReadOnlyList<Answer>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Answer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Answer>> GetByAssessmentIdAsync(Guid assessmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Answer>> GetByCandidateIdAsync(Guid candidateId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Answer>> GetByCandidateAndAssessmentAsync(Guid candidateId, Guid assessmentId, CancellationToken cancellationToken = default);
    Task<Answer?> GetByAssessmentCandidateAndQuestionAsync(Guid assessmentId, Guid candidateId, Guid questionId, CancellationToken cancellationToken = default);
    Task AddAsync(Answer answer, CancellationToken cancellationToken = default);
    void Update(Answer answer);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
