using IdentityService.Application.DTOs;
using IdentityService.Application.Services;
using IdentityService.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace IdentityService.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IJwtService _jwtService;
        public AuthController(IUserService userService, IJwtService jwtService)
        {
            _userService = userService;
            _jwtService = jwtService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (await _userService.UserExistsAsync(request.Email))
                return BadRequest("User already exists");
            var user = await _userService.RegisterAsync(request.Name, request.Email, request.Password, request.Role);
            var token = _jwtService.GenerateToken(user);
            return Ok(new AuthResponse
            {
                Id = user.Id,
                Token = token,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _userService.ValidateUserAsync(request.Email, request.Password);
            if (user == null)
                return Unauthorized("Invalid credentials");
            var token = _jwtService.GenerateToken(user);
            return Ok(new AuthResponse
            {
                Id = user.Id,
                Token = token,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role
            });
        }
    }
}
