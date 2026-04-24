using ResultService.Domain.Entities;

namespace ResultService.Application.Services;

public interface IResultService
{
    // Query methods
    Task<IReadOnlyList<Result>> GetAllAsync();
    Task<Result?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Result>> GetByCandidateIdAsync(Guid candidateId);
    Task<IReadOnlyList<Result>> GetByAssessmentIdAsync(Guid assessmentId);
    Task<Result?> GetByCandidateAndAssessmentAsync(Guid candidateId, Guid assessmentId);
    
    // Command methods
    Task<Result> CreateAsync(Result result);
    Task<Result> UpdateAsync(Result result);
    Task<bool> DeleteAsync(Guid id);
    
    // Calculation and publishing
    Task<Result> CalculateAndPublishAsync(Guid candidateId, Guid assessmentId);
    Task<Result> PublishResultAsync(Guid resultId);
    Task<IReadOnlyList<Result>> GetPassedCandidatesAsync(Guid assessmentId);
    Task<IReadOnlyList<Result>> GetFailedCandidatesAsync(Guid assessmentId);
}
