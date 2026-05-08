using CandidateService.Domain.Entities;

namespace CandidateService.Application.Repositories;

public interface ICandidateRepository
{
    Task<Candidate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Candidate?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Candidate>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Candidate candidate, CancellationToken cancellationToken = default);
    void Update(Candidate candidate);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
