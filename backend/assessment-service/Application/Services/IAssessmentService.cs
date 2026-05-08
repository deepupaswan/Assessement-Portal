using AssessmentService.Application.DTOs;
using AssessmentService.Domain.Entities;

namespace AssessmentService.Application.Services;

/// <summary>
/// Assessment service interface for assessment management.
/// </summary>
public interface IAssessmentService
{
    Task<Assessment> CreateAssessmentAsync(string title, string description, int durationMinutes, bool randomizeQuestions);
    Task<Assessment?> GetAssessmentByIdAsync(Guid id);
    
    /// <summary>
    /// Gets all assessments without questions (for listing).
    /// </summary>
    Task<PaginatedResponse<AssessmentListDto>> GetAllAssessmentsAsync(int pageNumber = 1, int pageSize = 20);
    
    /// <summary>
    /// Gets assessment with all questions and options (for detail view).
    /// </summary>
    Task<AssessmentDetailDto?> GetAssessmentDetailsAsync(Guid id);
    
    Task<bool> UpdateAssessmentAsync(Guid id, string title, string description, int durationMinutes, bool randomizeQuestions);
    Task<bool> DeleteAssessmentAsync(Guid id);
}
