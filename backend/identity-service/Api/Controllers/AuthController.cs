using IdentityService.Application.DTOs;
using IdentityService.Application.Services;
using IdentityService.Api.Events;
using IdentityService.Domain.Entities;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace IdentityService.Api.Controllers
{
    /// <summary>
    /// Authentication controller for user registration and login with rate limiting.
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IJwtService _jwtService;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IUserService userService,
            IJwtService jwtService,
            IPublishEndpoint publishEndpoint,
            ILogger<AuthController> logger)
        {
            _userService = userService;
            _jwtService = jwtService;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        /// <summary>
        /// Registers a new user with validation.
        /// </summary>
        [HttpPost("register")]
        [DisableRateLimiting]  // Register has less strict limits
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.Email) || string.IsNullOrWhiteSpace(request?.Password))
                    return BadRequest(new { message = "Email and password are required" });

                if (await _userService.UserExistsAsync(request.Email))
                {
                    _logger.LogWarning("Registration attempt for existing email: {Email}", request.Email);
                    return BadRequest(new { message = "User already exists" });
                }

                var user = await _userService.RegisterAsync(
                    request.Name,
                    request.Email,
                    request.Password,
                    request.Role);

                if (string.Equals(user.Role, "Candidate", StringComparison.OrdinalIgnoreCase))
                {
                    await _publishEndpoint.Publish(new CandidateRegisteredEvent
                    {
                        UserId = user.Id,
                        Name = user.Name,
                        Email = user.Email,
                        RegisteredAtUtc = user.RegisteredAt
                    });
                }

                var token = _jwtService.GenerateToken(user);
                _logger.LogInformation("User registered successfully: {Email}", request.Email);

                return Ok(new AuthResponse
                {
                    Id = user.Id,
                    Token = token,
                    Name = user.Name,
                    Email = user.Email,
                    Role = user.Role
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Registration error: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Registration validation error: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during registration for {Email}", request?.Email);
                throw;
            }
        }

        /// <summary>
        /// Authenticates user with email and password. Protected by rate limiting (5 attempts per 15 minutes).
        /// </summary>
        [HttpPost("login")]
        [EnableRateLimiting("login")]  // CRITICAL SECURITY: 5 attempts per 15 minutes
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.Email) || string.IsNullOrWhiteSpace(request?.Password))
                {
                    _logger.LogWarning("Login attempt with missing email or password");
                    return Unauthorized(new { message = "Invalid credentials" });
                }

                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                var user = await _userService.ValidateUserAsync(request.Email, request.Password);

                if (user == null)
                {
                    // Don't reveal if email exists (prevents account enumeration)
                    _logger.LogWarning("Failed login attempt - Email: {Email}, IP: {ClientIp}", 
                        request.Email, clientIp);
                    return Unauthorized(new { message = "Invalid credentials" });
                }

                var token = _jwtService.GenerateToken(user);
                _logger.LogInformation("Successful login: {Email} from {ClientIp}", 
                    user.Email, clientIp);

                return Ok(new AuthResponse
                {
                    Id = user.Id,
                    Token = token,
                    Name = user.Name,
                    Email = user.Email,
                    Role = user.Role
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login");
                throw;
            }
        }
    }
}
