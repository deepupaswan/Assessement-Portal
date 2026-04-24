using AssessmentService.Domain.Entities;
using AssessmentService.Infrastructure.Persistence;

namespace AssessmentService.Infrastructure.Services;

public class DataSeeder
{
    private readonly AssessmentDbContext _context;

    public DataSeeder(AssessmentDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync()
    {
        if (_context.Assessments.Any())
            return; // Already seeded

        // Create sample assessments with questions
        var assessment1 = new Assessment
        {
            Id = Guid.NewGuid(),
            Title = "C# Fundamentals",
            Description = "Test your knowledge of C# programming basics",
            CreatedAt = DateTime.UtcNow
        };

        var assessment2 = new Assessment
        {
            Id = Guid.NewGuid(),
            Title = "ASP.NET Core",
            Description = "Assess your ASP.NET Core skills",
            CreatedAt = DateTime.UtcNow
        };

        _context.Assessments.AddRange(assessment1, assessment2);
        await _context.SaveChangesAsync();

        // Add questions to assessment1 (C# Fundamentals)
        var q1 = new Question
        {
            Id = Guid.NewGuid(),
            AssessmentId = assessment1.Id,
            Text = "What is the correct way to declare a variable in C#?",
            Order = 1,
            CreatedAt = DateTime.UtcNow
        };

        var q2 = new Question
        {
            Id = Guid.NewGuid(),
            AssessmentId = assessment1.Id,
            Text = "Which of the following is a reference type in C#?",
            Order = 2,
            CreatedAt = DateTime.UtcNow
        };

        var q3 = new Question
        {
            Id = Guid.NewGuid(),
            AssessmentId = assessment1.Id,
            Text = "What does LINQ stand for?",
            Order = 3,
            CreatedAt = DateTime.UtcNow
        };

        _context.Questions.AddRange(q1, q2, q3);
        await _context.SaveChangesAsync();

        // Add options to q1
        _context.QuestionOptions.AddRange(
            new QuestionOption { Id = Guid.NewGuid(), QuestionId = q1.Id, Text = "int x;", IsCorrect = true, Order = 1 },
            new QuestionOption { Id = Guid.NewGuid(), QuestionId = q1.Id, Text = "x int;", IsCorrect = false, Order = 2 },
            new QuestionOption { Id = Guid.NewGuid(), QuestionId = q1.Id, Text = "int x =;", IsCorrect = false, Order = 3 },
            new QuestionOption { Id = Guid.NewGuid(), QuestionId = q1.Id, Text = "variable int x;", IsCorrect = false, Order = 4 }
        );

        // Add options to q2
        _context.QuestionOptions.AddRange(
            new QuestionOption { Id = Guid.NewGuid(), QuestionId = q2.Id, Text = "int", IsCorrect = false, Order = 1 },
            new QuestionOption { Id = Guid.NewGuid(), QuestionId = q2.Id, Text = "string", IsCorrect = true, Order = 2 },
            new QuestionOption { Id = Guid.NewGuid(), QuestionId = q2.Id, Text = "bool", IsCorrect = false, Order = 3 },
            new QuestionOption { Id = Guid.NewGuid(), QuestionId = q2.Id, Text = "double", IsCorrect = false, Order = 4 }
        );

        // Add options to q3
        _context.QuestionOptions.AddRange(
            new QuestionOption { Id = Guid.NewGuid(), QuestionId = q3.Id, Text = "Language Integrated Query", IsCorrect = true, Order = 1 },
            new QuestionOption { Id = Guid.NewGuid(), QuestionId = q3.Id, Text = "Linked Integer Query System", IsCorrect = false, Order = 2 },
            new QuestionOption { Id = Guid.NewGuid(), QuestionId = q3.Id, Text = "List Integration Quick Search", IsCorrect = false, Order = 3 },
            new QuestionOption { Id = Guid.NewGuid(), QuestionId = q3.Id, Text = "Logic Inference Query Layer", IsCorrect = false, Order = 4 }
        );

        // Add questions to assessment2 (ASP.NET Core)
        var q4 = new Question
        {
            Id = Guid.NewGuid(),
            AssessmentId = assessment2.Id,
            Text = "What is the default DI container in ASP.NET Core?",
            Order = 1,
            CreatedAt = DateTime.UtcNow
        };

        var q5 = new Question
        {
            Id = Guid.NewGuid(),
            AssessmentId = assessment2.Id,
            Text = "Which middleware is responsible for routing?",
            Order = 2,
            CreatedAt = DateTime.UtcNow
        };

        _context.Questions.Add(q4);
        _context.Questions.Add(q5);
        await _context.SaveChangesAsync();

        // Add options to q4
        _context.QuestionOptions.AddRange(
            new QuestionOption { Id = Guid.NewGuid(), QuestionId = q4.Id, Text = "Microsoft.Extensions.DependencyInjection", IsCorrect = true, Order = 1 },
            new QuestionOption { Id = Guid.NewGuid(), QuestionId = q4.Id, Text = "Autofac", IsCorrect = false, Order = 2 },
            new QuestionOption { Id = Guid.NewGuid(), QuestionId = q4.Id, Text = "Castle Windsor", IsCorrect = false, Order = 3 },
            new QuestionOption { Id = Guid.NewGuid(), QuestionId = q4.Id, Text = "StructureMap", IsCorrect = false, Order = 4 }
        );

        // Add options to q5
        _context.QuestionOptions.AddRange(
            new QuestionOption { Id = Guid.NewGuid(), QuestionId = q5.Id, Text = "app.UseRouting()", IsCorrect = true, Order = 1 },
            new QuestionOption { Id = Guid.NewGuid(), QuestionId = q5.Id, Text = "app.UseMapping()", IsCorrect = false, Order = 2 },
            new QuestionOption { Id = Guid.NewGuid(), QuestionId = q5.Id, Text = "app.UseNavigation()", IsCorrect = false, Order = 3 },
            new QuestionOption { Id = Guid.NewGuid(), QuestionId = q5.Id, Text = "app.UsePath()", IsCorrect = false, Order = 4 }
        );

        await _context.SaveChangesAsync();
    }
}
