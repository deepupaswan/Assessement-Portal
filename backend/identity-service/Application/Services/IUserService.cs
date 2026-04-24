namespace IdentityService.Application.Services
{
    public interface IUserService
    {
        Task<bool> UserExistsAsync(string email);
        Task<IdentityService.Domain.Entities.User> RegisterAsync(string name, string email, string password, string role);
        Task<IdentityService.Domain.Entities.User?> ValidateUserAsync(string email, string password);
    }
}