using CandidateService.Application.Services;
using CandidateService.Domain.Entities;
using CandidateService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CandidateService.Infrastructure.Services;

public class CandidateAssessmentService : ICandidateAssessmentService
{
    private readonly CandidateDbContext _context;

    public CandidateAssessmentService(CandidateDbContext context)
    {
        _context = context;
    }

    public async Task<CandidateAssessment> AssignAssessmentAsync(Guid candidateId, Guid assessmentId)
    {
        var assignment = new CandidateAssessment
        {
            Id = Guid.NewGuid(),
            CandidateId = candidateId,
            AssessmentId = assessmentId,
            AssignedAt = DateTime.UtcNow
        };

        _context.CandidateAssessments.Add(assignment);
        await _context.SaveChangesAsync();

        return assignment;
    }

    public async Task<CandidateAssessment?> GetAssignmentAsync(Guid id)
    {
        return await _context.CandidateAssessments
            .Include(ca => ca.Candidate)
            .FirstOrDefaultAsync(ca => ca.Id == id);
    }

    public async Task<IEnumerable<CandidateAssessment>> GetCandidateAssessmentsAsync(Guid candidateId)
    {
        return await _context.CandidateAssessments
            .Where(ca => ca.CandidateId == candidateId)
            .ToListAsync();
    }

    public async Task<bool> CompleteAssessmentAsync(Guid id)
    {
        var assignment = await _context.CandidateAssessments.FirstOrDefaultAsync(ca => ca.Id == id);
        if (assignment == null)
            return false;

        assignment.CompletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }
}
