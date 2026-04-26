using CandidateService.Application.Services;
using CandidateService.Domain.Entities;
using CandidateService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CandidateService.Infrastructure.Services;

public class CandidateService : ICandidateService
{
    private readonly CandidateDbContext _context;

    public CandidateService(CandidateDbContext context)
    {
        _context = context;
    }

    public async Task<Candidate> CreateCandidateAsync(string name, string email)
    {
        var candidate = new Candidate
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            CreatedAt = DateTime.UtcNow
        };

        _context.Candidates.Add(candidate);
        await _context.SaveChangesAsync();

        return candidate;
    }

    public async Task<Candidate?> GetCandidateByIdAsync(Guid id)
    {
        return await _context.Candidates.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Candidate?> GetCandidateByEmailAsync(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return await _context.Candidates.FirstOrDefaultAsync(c => c.Email.ToLower() == normalizedEmail);
    }

    public async Task<IEnumerable<Candidate>> GetAllCandidatesAsync()
    {
        return await _context.Candidates.ToListAsync();
    }

    public async Task<bool> UpdateCandidateAsync(Guid id, string name, string email)
    {
        var candidate = await _context.Candidates.FirstOrDefaultAsync(c => c.Id == id);
        if (candidate == null)
            return false;

        candidate.Name = name;
        candidate.Email = email;
        candidate.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteCandidateAsync(Guid id)
    {
        var candidate = await _context.Candidates.FirstOrDefaultAsync(c => c.Id == id);
        if (candidate == null)
            return false;

        _context.Candidates.Remove(candidate);
        await _context.SaveChangesAsync();
        return true;
    }
}
