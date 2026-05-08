namespace AssessmentService.Application.DTOs;

/// <summary>
/// Pagination request parameters.
/// </summary>
public class PaginationRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Validates and clamps pagination parameters.
    /// </summary>
    public void Validate()
    {
        if (PageNumber < 1) PageNumber = 1;
        if (PageSize < 1) PageSize = 20;
        if (PageSize > 100) PageSize = 100;  // Max 100 items per page
    }
}

/// <summary>
/// Paginated response with metadata.
/// </summary>
public class PaginatedResponse<T>
{
    public IReadOnlyList<T> Items { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
    public bool HasNextPage => PageNumber < TotalPages;
    public bool HasPreviousPage => PageNumber > 1;
}

/// <summary>
/// Assessment DTO for listing (without questions).
/// </summary>
public class AssessmentListDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public int TotalQuestions { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Assessment DTO for details (with questions).
/// </summary>
public class AssessmentDetailDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public bool RandomizeQuestions { get; set; }
    public bool IsPublished { get; set; }
    public List<AssessmentQuestionDetailDto> Questions { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class AssessmentQuestionDetailDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty;
    public int Order { get; set; }
    public List<AssessmentQuestionOptionDetailDto> Options { get; set; } = new();
}

public class AssessmentQuestionOptionDetailDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public int Order { get; set; }
}
