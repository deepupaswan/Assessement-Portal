using System.Security.Cryptography;
using System.Text;
using IdentityService.Application.Services;
using IdentityService.Domain.Entities;
using IdentityService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly IdentityDbContext _dbContext;

    public UserService(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> UserExistsAsync(string email)
        => await _dbContext.Users.AnyAsync(u => u.Email == email);

    public async Task<User> RegisterAsync(string name, string email, string password, string role)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            UserName = email,
            PasswordHash = HashPassword(password, email),
            Role = string.IsNullOrWhiteSpace(role) ? "Candidate" : role,
            RegisteredAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        return user;
    }

    public async Task<User?> ValidateUserAsync(string email, string password)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null)
        {
            return null;
        }

        return user.PasswordHash == HashPassword(password, email) ? user : null;
    }

    private static string HashPassword(string password, string salt)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{salt}:{password}"));
        return Convert.ToBase64String(bytes);
    }
}
