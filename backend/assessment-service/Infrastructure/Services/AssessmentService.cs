using AssessmentService.Application.DTOs;
using AssessmentService.Application.Repositories;
using AssessmentService.Application.Services;
using AssessmentService.Domain.Entities;

namespace AssessmentService.Infrastructure.Services;

/// <summary>
/// Assessment service with pagination support to prevent N+1 queries and memory issues.
/// </summary>
public class AssessmentService : IAssessmentService
{
    private readonly IAssessmentRepository _repository;

    public AssessmentService(IAssessmentRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Creates a new assessment.
    /// </summary>
    public async Task<Assessment> CreateAssessmentAsync(string title, string description, int durationMinutes, bool randomizeQuestions)
    {
        var assessment = new Assessment
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            DurationMinutes = Math.Max(durationMinutes, 1),
            RandomizeQuestions = randomizeQuestions,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(assessment);
        await _repository.SaveChangesAsync();

        return assessment;
    }

    /// <summary>
    /// Gets assessment by ID (includes questions and options).
    /// </summary>
    public async Task<Assessment?> GetAssessmentByIdAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id, includeQuestions: true);
    }

    /// <summary>
    /// Gets assessment details with full question hierarchy (for exam taking).
    /// </summary>
    public async Task<AssessmentDetailDto?> GetAssessmentDetailsAsync(Guid id)
    {
        var assessment = await _repository.GetByIdAsync(id);

        if (assessment == null)
            return null;

        // Load questions separately (avoid large joins)
        var questions = await _repository.GetQuestionsByAssessmentIdAsync(id);

        return new AssessmentDetailDto
        {
            Id = assessment.Id,
            Title = assessment.Title,
            Description = assessment.Description,
            DurationMinutes = assessment.DurationMinutes,
            RandomizeQuestions = assessment.RandomizeQuestions,
            IsPublished = assessment.IsPublished,
            CreatedAt = assessment.CreatedAt,
            UpdatedAt = assessment.UpdatedAt,
            Questions = questions.Select(q => new AssessmentQuestionDetailDto
            {
                Id = q.Id,
                Text = q.Text,
                QuestionType = q.Type,
                Order = q.Order,
                Options = q.Options.Select(o => new AssessmentQuestionOptionDetailDto
                {
                    Id = o.Id,
                    Text = o.Text,
                    Order = o.Order
                }).ToList()
            }).ToList()
        };
    }

    /// <summary>
    /// Gets paginated list of assessments WITHOUT questions (for listing).
    /// CRITICAL FIX: Prevents N+1 queries and memory explosion.
    /// </summary>
    public async Task<PaginatedResponse<AssessmentListDto>> GetAllAssessmentsAsync(int pageNumber = 1, int pageSize = 20)
    {
        // Validate pagination parameters
        var request = new PaginationRequest { PageNumber = pageNumber, PageSize = pageSize };
        request.Validate();

        // Get total count (separate query)
        var totalCount = await _repository.GetAssessmentCountAsync();

        if (totalCount == 0)
        {
            return new PaginatedResponse<AssessmentListDto>
            {
                Items = new List<AssessmentListDto>(),
                TotalCount = 0,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }

        // Get paginated assessments WITHOUT questions
        var assessments = await _repository.GetPagedAssessmentsAsync(
            (request.PageNumber - 1) * request.PageSize,
            request.PageSize);

        // Get question counts for these assessments (single query)
        var assessmentIds = assessments.Select(a => a.Id).ToList();
        var questionCountDict = await _repository.GetQuestionCountsByAssessmentIdsAsync(assessmentIds);

        // Map to DTOs
        var dtos = assessments.Select(a => new AssessmentListDto
        {
            Id = a.Id,
            Title = a.Title,
            Description = a.Description,
            DurationMinutes = a.DurationMinutes,
            TotalQuestions = questionCountDict.GetValueOrDefault(a.Id, 0),
            IsPublished = a.IsPublished,
            CreatedAt = a.CreatedAt,
            UpdatedAt = a.UpdatedAt
        }).ToList();

        return new PaginatedResponse<AssessmentListDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    /// <summary>
    /// Updates assessment details.
    /// </summary>
    public async Task<bool> UpdateAssessmentAsync(Guid id, string title, string description, int durationMinutes, bool randomizeQuestions)
    {
        var assessment = await _repository.GetByIdAsync(id, includeQuestions: false);
        if (assessment == null)
            return false;

        assessment.Title = title;
        assessment.Description = description;
        assessment.DurationMinutes = Math.Max(durationMinutes, 1);
        assessment.RandomizeQuestions = randomizeQuestions;
        assessment.UpdatedAt = DateTime.UtcNow;

        _repository.Update(assessment);
        await _repository.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Deletes assessment and cascades to related questions/answers.
    /// </summary>
    public async Task<bool> DeleteAssessmentAsync(Guid id)
    {
        var deleted = await _repository.DeleteAsync(id);
        if (!deleted)
            return false;

        await _repository.SaveChangesAsync();
        return true;
    }
}
