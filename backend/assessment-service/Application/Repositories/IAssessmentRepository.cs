using AssessmentService.Domain.Entities;

namespace AssessmentService.Application.Repositories;

public interface IAssessmentRepository
{
    Task<int> GetAssessmentCountAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Assessment>> GetPagedAssessmentsAsync(int skip, int take, CancellationToken cancellationToken = default);
    Task<Assessment?> GetByIdAsync(Guid id, bool includeQuestions = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Question>> GetQuestionsByAssessmentIdAsync(Guid assessmentId, CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, int>> GetQuestionCountsByAssessmentIdsAsync(IEnumerable<Guid> assessmentIds, CancellationToken cancellationToken = default);
    Task AddAsync(Assessment assessment, CancellationToken cancellationToken = default);
    void Update(Assessment assessment);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
