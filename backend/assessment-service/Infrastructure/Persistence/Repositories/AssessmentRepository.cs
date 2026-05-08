using AssessmentService.Application.Repositories;
using AssessmentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssessmentService.Infrastructure.Persistence.Repositories;

public class AssessmentRepository : IAssessmentRepository
{
    private readonly AssessmentDbContext _context;

    public AssessmentRepository(AssessmentDbContext context)
    {
        _context = context;
    }

    public Task<int> GetAssessmentCountAsync(CancellationToken cancellationToken = default)
        => _context.Assessments.AsNoTracking().CountAsync(cancellationToken);

    public async Task<IReadOnlyList<Assessment>> GetPagedAssessmentsAsync(int skip, int take, CancellationToken cancellationToken = default)
        => await _context.Assessments
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<Assessment?> GetByIdAsync(Guid id, bool includeQuestions = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Assessments.AsQueryable();
        if (includeQuestions)
        {
            query = query.Include(a => a.Questions);
        }

        return await query.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Question>> GetQuestionsByAssessmentIdAsync(Guid assessmentId, CancellationToken cancellationToken = default)
        => await _context.Questions
            .AsNoTracking()
            .Where(q => q.AssessmentId == assessmentId)
            .OrderBy(q => q.Order)
            .Include(q => q.Options.OrderBy(o => o.Order))
            .ToListAsync(cancellationToken);

    public async Task<Dictionary<Guid, int>> GetQuestionCountsByAssessmentIdsAsync(IEnumerable<Guid> assessmentIds, CancellationToken cancellationToken = default)
    {
        var ids = assessmentIds.ToList();
        var counts = await _context.Questions
            .AsNoTracking()
            .Where(q => ids.Contains(q.AssessmentId))
            .GroupBy(q => q.AssessmentId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(x => x.Key, x => x.Count);
    }

    public Task AddAsync(Assessment assessment, CancellationToken cancellationToken = default)
        => _context.Assessments.AddAsync(assessment, cancellationToken).AsTask();

    public void Update(Assessment assessment) => _context.Assessments.Update(assessment);

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var assessment = await _context.Assessments.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (assessment == null)
        {
            return false;
        }

        _context.Assessments.Remove(assessment);
        return true;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
