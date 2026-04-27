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

    public async Task<CandidateAssessment> AssignAssessmentAsync(Guid candidateId, Guid assessmentId, DateTime? scheduledAtUtc = null)
    {
        var assignment = new CandidateAssessment
        {
            Id = Guid.NewGuid(),
            CandidateId = candidateId,
            AssessmentId = assessmentId,
            AssignedAt = DateTime.UtcNow,
            ScheduledAtUtc = NormalizeUtc(scheduledAtUtc)
        };

        _context.CandidateAssessments.Add(assignment);
        await _context.SaveChangesAsync();
        await _context.Entry(assignment).Reference(item => item.Candidate).LoadAsync();

        return assignment;
    }

    public async Task<IEnumerable<CandidateAssessment>> GetAllAssignmentsAsync()
    {
        return await _context.CandidateAssessments
            .Include(ca => ca.Candidate)
            .OrderByDescending(ca => ca.ScheduledAtUtc ?? ca.AssignedAt)
            .ToListAsync();
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
            .Include(ca => ca.Candidate)
            .Where(ca => ca.CandidateId == candidateId)
            .ToListAsync();
    }

    public async Task<CandidateAssessment?> UpdateAssignmentAsync(Guid id, Guid candidateId, Guid assessmentId, DateTime? scheduledAtUtc)
    {
        var assignment = await _context.CandidateAssessments
            .Include(ca => ca.Candidate)
            .FirstOrDefaultAsync(ca => ca.Id == id);

        if (assignment == null)
            return null;

        if (assignment.StartedAtUtc.HasValue || assignment.CompletedAt.HasValue)
            throw new InvalidOperationException("Only pending assignments can be updated.");

        assignment.CandidateId = candidateId;
        assignment.AssessmentId = assessmentId;
        assignment.ScheduledAtUtc = NormalizeUtc(scheduledAtUtc);

        await _context.SaveChangesAsync();
        await _context.Entry(assignment).Reference(item => item.Candidate).LoadAsync();

        return assignment;
    }

    public async Task<bool> StartAssessmentAsync(Guid id)
    {
        var assignment = await _context.CandidateAssessments.FirstOrDefaultAsync(ca => ca.Id == id);
        if (assignment == null)
            return false;

        if (!assignment.StartedAtUtc.HasValue)
            assignment.StartedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> CompleteAssessmentAsync(Guid id)
    {
        var assignment = await _context.CandidateAssessments.FirstOrDefaultAsync(ca => ca.Id == id);
        if (assignment == null)
            return false;

        if (!assignment.StartedAtUtc.HasValue)
            assignment.StartedAtUtc = DateTime.UtcNow;

        assignment.CompletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAssignmentAsync(Guid id)
    {
        var assignment = await _context.CandidateAssessments.FirstOrDefaultAsync(ca => ca.Id == id);
        if (assignment == null)
            return false;

        if (assignment.StartedAtUtc.HasValue || assignment.CompletedAt.HasValue)
            throw new InvalidOperationException("Only pending assignments can be deleted.");

        _context.CandidateAssessments.Remove(assignment);
        await _context.SaveChangesAsync();

        return true;
    }

    private static DateTime? NormalizeUtc(DateTime? value)
    {
        if (!value.HasValue)
            return null;

        return value.Value.Kind switch
        {
            DateTimeKind.Utc => value.Value,
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
        };
    }
}
