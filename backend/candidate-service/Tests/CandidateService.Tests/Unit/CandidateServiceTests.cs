using CandidateService.Application.Repositories;
using CandidateService.Application.Services;
using CandidateService.Domain.Entities;
using CandidateService.Infrastructure.Services;
using Moq;
using Xunit;

namespace CandidateService.Tests.Unit;

public class CandidateServiceTests
{
    [Fact]
    public async Task CreateCandidateAsync_IndexesAfterPersistence()
    {
        var repository = new Mock<ICandidateRepository>();
        var searchService = new Mock<ICandidateSearchService>();
        var sut = new CandidateService.Infrastructure.Services.CandidateService(repository.Object, searchService.Object);

        var result = await sut.CreateCandidateAsync("Test User", "test@example.com");

        Assert.Equal("Test User", result.Name);
        repository.Verify(r => r.AddAsync(It.IsAny<Candidate>(), It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        searchService.Verify(s => s.IndexCandidateAsync(It.IsAny<Candidate>()), Times.Once);
    }
}
