using IdentityService.Application.Repositories;
using IdentityService.Application.Services;
using IdentityService.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace IdentityService.Infrastructure.Services;

/// <summary>
/// User service with secure password hashing using bcrypt.
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService> _logger;
    private const int BcryptWorkFactor = 12;  // ~100ms per hash - increase over time as hardware improves

    public UserService(IUserRepository userRepository, ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    /// <summary>
    /// Checks if user exists by email.
    /// </summary>
    public async Task<bool> UserExistsAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;
        
        return await _userRepository.ExistsByEmailAsync(email.ToLower());
    }

    /// <summary>
    /// Registers a new user with secure bcrypt password hashing.
    /// </summary>
    public async Task<User> RegisterAsync(string name, string email, string password, string role)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Email and password are required");
        
        var normalizedEmail = email.ToLower().Trim();
        
        if (await UserExistsAsync(normalizedEmail))
            throw new InvalidOperationException("User with this email already exists");
        
        // Hash password using bcrypt (automatic random salt + work factor)
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: BcryptWorkFactor);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = name?.Trim() ?? "",
            Email = normalizedEmail,
            UserName = normalizedEmail,
            PasswordHash = passwordHash,  // ~60 chars, includes salt & work factor
            Role = string.IsNullOrWhiteSpace(role) ? "Candidate" : role.Trim(),
            RegisteredAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();
        
        _logger.LogInformation("User registered: {Email}", normalizedEmail);
        return user;
    }

    /// <summary>
    /// Validates user credentials with constant-time password comparison.
    /// </summary>
    public async Task<User?> ValidateUserAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return null;
        
        var normalizedEmail = email.ToLower().Trim();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, asNoTracking: false);
        
        if (user is null)
        {
            _logger.LogWarning("Login attempt for non-existent email: {Email}", normalizedEmail);
            return null;
        }

        // Bcrypt.Verify uses constant-time comparison (prevents timing attacks)
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for user: {Email}", normalizedEmail);
            return null;
        }

        // IMPORTANT: Detect weak hashes and upgrade on login
        if (!BCrypt.Net.BCrypt.EnhancedVerify(password, user.PasswordHash))
        {
            _logger.LogInformation("Upgrading password hash for user: {Email}", normalizedEmail);
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: BcryptWorkFactor);
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();
        }

        _logger.LogInformation("Successful login for user: {Email}", normalizedEmail);
        return user;
    }
}
