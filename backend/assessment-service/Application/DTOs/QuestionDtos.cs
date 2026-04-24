namespace AssessmentService.Application.DTOs;

public class QuestionDto
{
    public Guid Id { get; set; }
    public Guid AssessmentId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Type { get; set; } = "MCQ";
    public int MaxScore { get; set; }
    public string? CorrectAnswer { get; set; }
    public bool IsRequired { get; set; }
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<QuestionOptionDto> Options { get; set; } = new();
}

public class QuestionOptionDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int Order { get; set; }
}

public class CreateQuestionRequest
{
    public string Text { get; set; } = string.Empty;
    public string Type { get; set; } = "MCQ";
    public int MaxScore { get; set; } = 1;
    public string? CorrectAnswer { get; set; }
    public bool IsRequired { get; set; } = true;
    public int Order { get; set; }
    public List<CreateQuestionOptionRequest> Options { get; set; } = new();
}

public class CreateQuestionOptionRequest
{
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int Order { get; set; }
}

public class UpdateQuestionRequest
{
    public string Text { get; set; } = string.Empty;
    public string Type { get; set; } = "MCQ";
    public int MaxScore { get; set; } = 1;
    public string? CorrectAnswer { get; set; }
    public bool IsRequired { get; set; } = true;
    public int Order { get; set; }
}

public class UpdateQuestionOptionRequest
{
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int Order { get; set; }
}
