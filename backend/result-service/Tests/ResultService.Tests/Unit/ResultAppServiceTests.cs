using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using ResultService.Application.Events;
using ResultService.Application.Repositories;
using ResultService.Domain.Entities;
using ResultService.Infrastructure.Services;
using Xunit;

namespace ResultService.Tests.Unit;

public class ResultAppServiceTests
{
    [Fact]
    public async Task CreateAsync_PersistsAndPublishesEvent()
    {
        var repository = new Mock<IResultRepository>();
        var publisher = new Mock<IPublishEndpoint>();
        var logger = new Mock<ILogger<ResultAppService>>();
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["ServiceUrls:AnswerService"]).Returns("http://localhost:5118");
        config.Setup(c => c["ServiceUrls:AssessmentService"]).Returns("http://localhost:5098");

        var sut = new ResultAppService(
            repository.Object,
            publisher.Object,
            logger.Object,
            new HttpClient(),
            config.Object);

        var result = await sut.CreateAsync(new Result
        {
            CandidateId = Guid.NewGuid(),
            AssessmentId = Guid.NewGuid(),
            Score = 10
        });

        Assert.NotEqual(Guid.Empty, result.Id);
        repository.Verify(r => r.AddAsync(It.IsAny<Result>(), It.IsAny<CancellationToken>()), Times.Once);
        publisher.Verify(p => p.Publish(It.IsAny<ResultCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
