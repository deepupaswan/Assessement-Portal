using CandidateService.Domain.Entities;

namespace CandidateService.Application.Services;

public interface ICandidateAssessmentService
{
    Task<CandidateAssessment> AssignAssessmentAsync(Guid candidateId, Guid assessmentId);
    Task<CandidateAssessment?> GetAssignmentAsync(Guid id);
    Task<IEnumerable<CandidateAssessment>> GetCandidateAssessmentsAsync(Guid candidateId);
    Task<bool> CompleteAssessmentAsync(Guid id);
}
