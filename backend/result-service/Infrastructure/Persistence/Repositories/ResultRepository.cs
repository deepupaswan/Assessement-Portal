using Microsoft.EntityFrameworkCore;
using ResultService.Application.Repositories;
using ResultService.Domain.Entities;

namespace ResultService.Infrastructure.Persistence.Repositories;

public class ResultRepository : IResultRepository
{
    private readonly ResultDbContext _dbContext;

    public ResultRepository(ResultDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Result>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _dbContext.Results.AsNoTracking().ToListAsync(cancellationToken);

    public Task<Result?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.Results.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Result>> GetByCandidateIdAsync(Guid candidateId, CancellationToken cancellationToken = default)
        => await _dbContext.Results.AsNoTracking().Where(r => r.CandidateId == candidateId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Result>> GetByAssessmentIdAsync(Guid assessmentId, CancellationToken cancellationToken = default)
        => await _dbContext.Results.AsNoTracking().Where(r => r.AssessmentId == assessmentId).ToListAsync(cancellationToken);

    public Task<Result?> GetByCandidateAndAssessmentAsync(Guid candidateId, Guid assessmentId, bool asNoTracking = true, CancellationToken cancellationToken = default)
    {
        var query = asNoTracking ? _dbContext.Results.AsNoTracking() : _dbContext.Results.AsQueryable();
        return query.FirstOrDefaultAsync(r => r.CandidateId == candidateId && r.AssessmentId == assessmentId, cancellationToken);
    }

    public Task AddAsync(Result result, CancellationToken cancellationToken = default)
        => _dbContext.Results.AddAsync(result, cancellationToken).AsTask();

    public void Update(Result result) => _dbContext.Results.Update(result);

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _dbContext.Results.FindAsync(new object[] { id }, cancellationToken);
        if (result == null)
        {
            return false;
        }

        _dbContext.Results.Remove(result);
        return true;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
