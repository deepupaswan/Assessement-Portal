using CandidateService.Application.Repositories;
using CandidateService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CandidateService.Infrastructure.Persistence.Repositories;

public class CandidateRepository : ICandidateRepository
{
    private readonly CandidateDbContext _context;

    public CandidateRepository(CandidateDbContext context)
    {
        _context = context;
    }

    public Task<Candidate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Candidates.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Candidate?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        => _context.Candidates.AsNoTracking().FirstOrDefaultAsync(c => c.Email.ToLower() == normalizedEmail, cancellationToken);

    public async Task<IReadOnlyList<Candidate>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.Candidates.AsNoTracking().ToListAsync(cancellationToken);

    public Task AddAsync(Candidate candidate, CancellationToken cancellationToken = default)
        => _context.Candidates.AddAsync(candidate, cancellationToken).AsTask();

    public void Update(Candidate candidate) => _context.Candidates.Update(candidate);

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var candidate = await _context.Candidates.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (candidate == null)
        {
            return false;
        }

        _context.Candidates.Remove(candidate);
        return true;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
