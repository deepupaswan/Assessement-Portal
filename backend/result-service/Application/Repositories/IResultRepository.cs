using ResultService.Domain.Entities;

namespace ResultService.Application.Repositories;

public interface IResultRepository
{
    Task<IReadOnlyList<Result>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Result>> GetByCandidateIdAsync(Guid candidateId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Result>> GetByAssessmentIdAsync(Guid assessmentId, CancellationToken cancellationToken = default);
    Task<Result?> GetByCandidateAndAssessmentAsync(Guid candidateId, Guid assessmentId, bool asNoTracking = true, CancellationToken cancellationToken = default);
    Task AddAsync(Result result, CancellationToken cancellationToken = default);
    void Update(Result result);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
