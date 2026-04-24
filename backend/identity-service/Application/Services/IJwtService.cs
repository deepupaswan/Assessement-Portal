namespace IdentityService.Application.Services
{
    public interface IJwtService
    {
        string GenerateToken(IdentityService.Domain.Entities.User user);
    }
}