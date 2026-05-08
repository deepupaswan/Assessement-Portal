using CandidateService.Domain.Entities;

namespace CandidateService.Application.Services;

public interface ICandidateSearchService
{
    Task IndexCandidateAsync(Candidate candidate);
    Task DeleteCandidateAsync(Guid candidateId);
    Task<CandidateSearchResult> SearchCandidatesAsync(
        string query,
        int page = 1,
        int size = 20,
        string? email = null,
        DateTime? createdFromUtc = null,
        DateTime? createdToUtc = null,
        string? sortBy = null,
        string? sortOrder = null);
    Task<CandidateReindexResult> ReindexCandidatesAsync(IEnumerable<Candidate> candidates);
    Task ResetIndexAsync();
}

public sealed record CandidateReindexResult(int Total, int Indexed, int Failed);

public sealed record CandidateSearchResult(
    int Total,
    int Page,
    int Size,
    IReadOnlyList<Candidate> Items);