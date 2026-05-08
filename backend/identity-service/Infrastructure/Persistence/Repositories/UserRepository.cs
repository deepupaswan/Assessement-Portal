using IdentityService.Application.Repositories;
using IdentityService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _dbContext;

    public UserRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        => _dbContext.Users.AsNoTracking().AnyAsync(u => u.Email == normalizedEmail, cancellationToken);

    public async Task<User?> GetByEmailAsync(string normalizedEmail, bool asNoTracking = true, CancellationToken cancellationToken = default)
    {
        var query = asNoTracking ? _dbContext.Users.AsNoTracking() : _dbContext.Users.AsQueryable();
        return await query.FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);
    }

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
        => _dbContext.Users.AddAsync(user, cancellationToken).AsTask();

    public void Update(User user) => _dbContext.Users.Update(user);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
