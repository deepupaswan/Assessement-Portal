using CandidateService.Domain.Entities;

namespace CandidateService.Application.Services;

public interface ICandidateService
{
    Task<Candidate> CreateCandidateAsync(string name, string email);
    Task<Candidate?> GetCandidateByIdAsync(Guid id);
    Task<Candidate?> GetCandidateByEmailAsync(string email);
    Task<IEnumerable<Candidate>> GetAllCandidatesAsync();
    Task<bool> UpdateCandidateAsync(Guid id, string name, string email);
    Task<bool> DeleteCandidateAsync(Guid id);
}
