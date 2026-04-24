using AssessmentService.Domain.Entities;

namespace AssessmentService.Application.Services;

public interface IQuestionService
{
    Task<Question> CreateQuestionAsync(Guid assessmentId, string text, string type, int maxScore, string? correctAnswer, bool isRequired, int order);
    Task<Question?> GetQuestionByIdAsync(Guid id);
    Task<IEnumerable<Question>> GetQuestionsByAssessmentIdAsync(Guid assessmentId);
    Task<bool> UpdateQuestionAsync(Guid id, string text, string type, int maxScore, string? correctAnswer, bool isRequired, int order);
    Task<bool> DeleteQuestionAsync(Guid id);
    
    // Options management
    Task<QuestionOption> AddOptionAsync(Guid questionId, string text, bool isCorrect, int order);
    Task<bool> UpdateOptionAsync(Guid optionId, string text, bool isCorrect, int order);
    Task<bool> DeleteOptionAsync(Guid optionId);
    Task<IEnumerable<QuestionOption>> GetOptionsByQuestionIdAsync(Guid questionId);
}
