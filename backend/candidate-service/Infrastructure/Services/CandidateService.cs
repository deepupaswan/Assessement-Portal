using CandidateService.Application.Repositories;
using CandidateService.Application.Services;
using CandidateService.Domain.Entities;

namespace CandidateService.Infrastructure.Services;

public class CandidateService : ICandidateService
{
    private readonly ICandidateRepository _repository;
    private readonly ICandidateSearchService _candidateSearchService;

    public CandidateService(ICandidateRepository repository, ICandidateSearchService candidateSearchService)
    {
        _repository = repository;
        _candidateSearchService = candidateSearchService;
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

        await _repository.AddAsync(candidate);
        await _repository.SaveChangesAsync();
        await _candidateSearchService.IndexCandidateAsync(candidate);

        return candidate;
    }

    public async Task<Candidate?> GetCandidateByIdAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<Candidate?> GetCandidateByEmailAsync(string email)
    {
        // Normalize email once - database has case-insensitive index
        var normalizedEmail = email?.Trim().ToLowerInvariant() ?? "";
        return await _repository.GetByEmailAsync(normalizedEmail);
    }

    public async Task<IEnumerable<Candidate>> GetAllCandidatesAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<bool> UpdateCandidateAsync(Guid id, string name, string email)
    {
        var candidate = await _repository.GetByIdAsync(id);
        if (candidate == null)
            return false;

        candidate.Name = name;
        candidate.Email = email;
        candidate.UpdatedAt = DateTime.UtcNow;

        _repository.Update(candidate);
        await _repository.SaveChangesAsync();
        await _candidateSearchService.IndexCandidateAsync(candidate);
        return true;
    }

    public async Task<bool> DeleteCandidateAsync(Guid id)
    {
        var deleted = await _repository.DeleteAsync(id);
        if (!deleted)
            return false;

        await _repository.SaveChangesAsync();
        await _candidateSearchService.DeleteCandidateAsync(id);
        return true;
    }
}
