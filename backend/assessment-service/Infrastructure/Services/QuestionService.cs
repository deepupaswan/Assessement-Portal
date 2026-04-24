using AssessmentService.Application.Services;
using AssessmentService.Domain.Entities;
using AssessmentService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AssessmentService.Infrastructure.Services;

public class QuestionService : IQuestionService
{
    private readonly AssessmentDbContext _context;

    public QuestionService(AssessmentDbContext context)
    {
        _context = context;
    }

    public async Task<Question> CreateQuestionAsync(Guid assessmentId, string text, string type, int maxScore, string? correctAnswer, bool isRequired, int order)
    {
        var assessmentExists = await _context.Assessments.AnyAsync(a => a.Id == assessmentId);
        if (!assessmentExists)
            throw new ArgumentException("Assessment not found", nameof(assessmentId));

        var question = new Question
        {
            Id = Guid.NewGuid(),
            AssessmentId = assessmentId,
            Text = text,
            Type = string.IsNullOrWhiteSpace(type) ? "MCQ" : type.Trim().ToUpperInvariant(),
            MaxScore = Math.Max(maxScore, 1),
            CorrectAnswer = correctAnswer,
            IsRequired = isRequired,
            Order = order,
            CreatedAt = DateTime.UtcNow
        };

        _context.Questions.Add(question);
        await _context.SaveChangesAsync();

        return question;
    }

    public async Task<Question?> GetQuestionByIdAsync(Guid id)
    {
        return await _context.Questions
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == id);
    }

    public async Task<IEnumerable<Question>> GetQuestionsByAssessmentIdAsync(Guid assessmentId)
    {
        return await _context.Questions
            .Include(q => q.Options)
            .Where(q => q.AssessmentId == assessmentId)
            .OrderBy(q => q.Order)
            .ToListAsync();
    }

    public async Task<bool> UpdateQuestionAsync(Guid id, string text, string type, int maxScore, string? correctAnswer, bool isRequired, int order)
    {
        var question = await _context.Questions.FirstOrDefaultAsync(q => q.Id == id);
        if (question == null)
            return false;

        question.Text = text;
        question.Type = string.IsNullOrWhiteSpace(type) ? "MCQ" : type.Trim().ToUpperInvariant();
        question.MaxScore = Math.Max(maxScore, 1);
        question.CorrectAnswer = correctAnswer;
        question.IsRequired = isRequired;
        question.Order = order;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteQuestionAsync(Guid id)
    {
        var question = await _context.Questions.FirstOrDefaultAsync(q => q.Id == id);
        if (question == null)
            return false;

        _context.Questions.Remove(question);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<QuestionOption> AddOptionAsync(Guid questionId, string text, bool isCorrect, int order)
    {
        var option = new QuestionOption
        {
            Id = Guid.NewGuid(),
            QuestionId = questionId,
            Text = text,
            IsCorrect = isCorrect,
            Order = order
        };

        _context.QuestionOptions.Add(option);
        await _context.SaveChangesAsync();

        return option;
    }

    public async Task<bool> UpdateOptionAsync(Guid optionId, string text, bool isCorrect, int order)
    {
        var option = await _context.QuestionOptions.FirstOrDefaultAsync(o => o.Id == optionId);
        if (option == null)
            return false;

        option.Text = text;
        option.IsCorrect = isCorrect;
        option.Order = order;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteOptionAsync(Guid optionId)
    {
        var option = await _context.QuestionOptions.FirstOrDefaultAsync(o => o.Id == optionId);
        if (option == null)
            return false;

        _context.QuestionOptions.Remove(option);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<QuestionOption>> GetOptionsByQuestionIdAsync(Guid questionId)
    {
        return await _context.QuestionOptions
            .Where(o => o.QuestionId == questionId)
            .OrderBy(o => o.Order)
            .ToListAsync();
    }
}
