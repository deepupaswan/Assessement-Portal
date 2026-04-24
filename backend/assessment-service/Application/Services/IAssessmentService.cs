using AssessmentService.Domain.Entities;

namespace AssessmentService.Application.Services;

public interface IAssessmentService
{
    Task<Assessment> CreateAssessmentAsync(string title, string description, int durationMinutes, bool randomizeQuestions);
    Task<Assessment?> GetAssessmentByIdAsync(Guid id);
    Task<IEnumerable<Assessment>> GetAllAssessmentsAsync();
    Task<bool> UpdateAssessmentAsync(Guid id, string title, string description, int durationMinutes, bool randomizeQuestions);
    Task<bool> DeleteAssessmentAsync(Guid id);
}
