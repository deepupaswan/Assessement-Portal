using IdentityService.Application.Repositories;
using IdentityService.Domain.Entities;
using IdentityService.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IdentityService.Tests.Unit;

public class UserServiceTests
{
    [Fact]
    public async Task RegisterAsync_CreatesBcryptHash()
    {
        var repository = new Mock<IUserRepository>();
        repository.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var logger = new Mock<ILogger<UserService>>();
        var sut = new UserService(repository.Object, logger.Object);

        var user = await sut.RegisterAsync("Name", "user@example.com", "Pass@123456", "Candidate");

        Assert.NotEqual("Pass@123456", user.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("Pass@123456", user.PasswordHash));
        repository.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
