using AnswerService.Application.Repositories;
using AnswerService.Domain.Entities;
using AnswerService.Infrastructure.Services;
using MassTransit;
using Moq;
using Xunit;

namespace AnswerService.Tests.Unit;

public class AnswerAppServiceTests
{
    private readonly Mock<IAnswerRepository> _repositoryMock = new();
    private readonly Mock<IPublishEndpoint> _publishEndpointMock = new();

    [Fact]
    public async Task SubmitAnswerAsync_CreatesNewAnswer_WhenNoExistingAnswer()
    {
        // Arrange
        var assessmentId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByAssessmentCandidateAndQuestionAsync(assessmentId, candidateId, questionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Answer?)null);

        var sut = new AnswerAppService(_repositoryMock.Object, _publishEndpointMock.Object);

        // Act
        var result = await sut.SubmitAnswerAsync(assessmentId, candidateId, questionId, null, "  Sample text  ");

        // Assert
        Assert.Equal(assessmentId, result.AssessmentId);
        Assert.Equal(candidateId, result.CandidateId);
        Assert.Equal(questionId, result.QuestionId);
        Assert.Equal("Sample text", result.AnswerText);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Answer>(), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _publishEndpointMock.Verify(
            p => p.Publish(It.IsAny<AnswerService.Application.Events.AnswerCreatedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenAnswerMissing()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var sut = new AnswerAppService(_repositoryMock.Object, _publishEndpointMock.Object);

        // Act
        var deleted = await sut.DeleteAsync(Guid.NewGuid());

        // Assert
        Assert.False(deleted);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
