using AnswerService.Domain.Entities;
using AnswerService.Infrastructure.Persistence;
using AnswerService.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AnswerService.Tests.Integration;

public class AnswerRepositoryTests
{
    [Fact]
    public async Task GetByCandidateAndAssessmentAsync_ReturnsOnlyMatchingAnswers()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AnswerDbContext>()
            .UseInMemoryDatabase(databaseName: $"answers-{Guid.NewGuid()}")
            .Options;

        await using var context = new AnswerDbContext(options);
        var candidateId = Guid.NewGuid();
        var assessmentId = Guid.NewGuid();

        context.Answers.AddRange(
            new Answer
            {
                Id = Guid.NewGuid(),
                CandidateId = candidateId,
                AssessmentId = assessmentId,
                QuestionId = Guid.NewGuid(),
                AnswerText = "A1",
                QuestionText = "Q1",
                SubmittedAt = DateTime.UtcNow
            },
            new Answer
            {
                Id = Guid.NewGuid(),
                CandidateId = Guid.NewGuid(),
                AssessmentId = assessmentId,
                QuestionId = Guid.NewGuid(),
                AnswerText = "A2",
                QuestionText = "Q2",
                SubmittedAt = DateTime.UtcNow
            });
        await context.SaveChangesAsync();

        var repository = new AnswerRepository(context);

        // Act
        var results = await repository.GetByCandidateAndAssessmentAsync(candidateId, assessmentId);

        // Assert
        Assert.Single(results);
        Assert.Equal("A1", results[0].AnswerText);
    }
}
