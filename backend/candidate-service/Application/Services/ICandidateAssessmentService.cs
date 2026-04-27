using CandidateService.Domain.Entities;

namespace CandidateService.Application.Services;

public interface ICandidateAssessmentService
{
    Task<CandidateAssessment> AssignAssessmentAsync(Guid candidateId, Guid assessmentId, DateTime? scheduledAtUtc = null);
    Task<IEnumerable<CandidateAssessment>> GetAllAssignmentsAsync();
    Task<CandidateAssessment?> GetAssignmentAsync(Guid id);
    Task<IEnumerable<CandidateAssessment>> GetCandidateAssessmentsAsync(Guid candidateId);
    Task<CandidateAssessment?> UpdateAssignmentAsync(Guid id, Guid candidateId, Guid assessmentId, DateTime? scheduledAtUtc);
    Task<bool> StartAssessmentAsync(Guid id);
    Task<bool> CompleteAssessmentAsync(Guid id);
    Task<bool> DeleteAssignmentAsync(Guid id);
}
