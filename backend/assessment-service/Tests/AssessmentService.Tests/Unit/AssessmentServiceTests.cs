using AssessmentService.Application.Repositories;
using AssessmentService.Domain.Entities;
using AssessmentService.Infrastructure.Services;
using Moq;
using Xunit;

namespace AssessmentService.Tests.Unit;

public class AssessmentServiceTests
{
    [Fact]
    public async Task DeleteAssessmentAsync_ReturnsFalse_WhenAssessmentMissing()
    {
        var repository = new Mock<IAssessmentRepository>();
        repository.Setup(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var sut = new AssessmentService.Infrastructure.Services.AssessmentService(repository.Object);

        var deleted = await sut.DeleteAssessmentAsync(Guid.NewGuid());

        Assert.False(deleted);
        repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAssessmentAsync_PersistsAssessment()
    {
        var repository = new Mock<IAssessmentRepository>();
        var sut = new AssessmentService.Infrastructure.Services.AssessmentService(repository.Object);

        var created = await sut.CreateAssessmentAsync("Title", "Description", 30, true);

        Assert.Equal("Title", created.Title);
        repository.Verify(r => r.AddAsync(It.IsAny<Assessment>(), It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
