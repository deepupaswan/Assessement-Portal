using AssessmentService.Application.Services;
using AssessmentService.Domain.Entities;
using AssessmentService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AssessmentService.Infrastructure.Services;

public class AssessmentService : IAssessmentService
{
    private readonly AssessmentDbContext _context;

    public AssessmentService(AssessmentDbContext context)
    {
        _context = context;
    }

    public async Task<Assessment> CreateAssessmentAsync(string title, string description, int durationMinutes, bool randomizeQuestions)
    {
        var assessment = new Assessment
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            DurationMinutes = Math.Max(durationMinutes, 1),
            RandomizeQuestions = randomizeQuestions,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Assessments.Add(assessment);
        await _context.SaveChangesAsync();

        return assessment;
    }

    public async Task<Assessment?> GetAssessmentByIdAsync(Guid id)
    {
        return await _context.Assessments
            .Include(a => a.Questions)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<IEnumerable<Assessment>> GetAllAssessmentsAsync()
    {
        return await _context.Assessments
            .Include(a => a.Questions)
            .ToListAsync();
    }

    public async Task<bool> UpdateAssessmentAsync(Guid id, string title, string description, int durationMinutes, bool randomizeQuestions)
    {
        var assessment = await _context.Assessments.FirstOrDefaultAsync(a => a.Id == id);
        if (assessment == null)
            return false;

        assessment.Title = title;
        assessment.Description = description;
        assessment.DurationMinutes = Math.Max(durationMinutes, 1);
        assessment.RandomizeQuestions = randomizeQuestions;
        assessment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAssessmentAsync(Guid id)
    {
        var assessment = await _context.Assessments.FirstOrDefaultAsync(a => a.Id == id);
        if (assessment == null)
            return false;

        _context.Assessments.Remove(assessment);
        await _context.SaveChangesAsync();
        return true;
    }
}
