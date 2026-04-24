using AnswerService.Domain.Entities;

namespace AnswerService.Application.Services;

public interface IAnswerService
{
    // Query methods
    Task<IReadOnlyList<Answer>> GetAllAsync();
    Task<Answer?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Answer>> GetByAssessmentIdAsync(Guid assessmentId);
    Task<IReadOnlyList<Answer>> GetByCandidateIdAsync(Guid candidateId);
    Task<IReadOnlyList<Answer>> GetByCandidateAndAssessmentAsync(Guid candidateId, Guid assessmentId);
    
    // Command methods
    Task<Answer> CreateAsync(Answer answer);
    Task<Answer> UpdateAsync(Answer answer);
    Task<bool> DeleteAsync(Guid id);
    Task<Answer> SubmitAnswerAsync(Guid assessmentId, Guid candidateId, Guid questionId, Guid? selectedOptionId, string answerText);
    Task<Answer> GradeAnswerAsync(Guid answerId, bool isCorrect, int pointsObtained, string? notes = null);
}
