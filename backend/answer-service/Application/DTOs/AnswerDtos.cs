namespace AnswerService.Application.DTOs;

public class AnswerDto
{
    public Guid Id { get; set; }
    public Guid AssessmentId { get; set; }
    public Guid CandidateId { get; set; }
    public Guid QuestionId { get; set; }
    public Guid? SelectedOptionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string AnswerText { get; set; } = string.Empty;
    public bool? IsCorrect { get; set; }
    public int? PointsObtained { get; set; }
    public int? TotalPoints { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? GradedAt { get; set; }
    public string GradingNotes { get; set; } = string.Empty;
}

public class SubmitAnswerRequest
{
    public Guid AssessmentId { get; set; }
    public Guid CandidateId { get; set; }
    public Guid QuestionId { get; set; }
    public Guid? SelectedOptionId { get; set; }
    public string AnswerText { get; set; } = string.Empty;
}

public class GradeAnswerRequest
{
    public Guid AnswerId { get; set; }
    public bool IsCorrect { get; set; }
    public int PointsObtained { get; set; }
    public string? Notes { get; set; }
}

public class BatchSubmitAnswersRequest
{
    public Guid AssessmentId { get; set; }
    public Guid CandidateId { get; set; }
    public List<SubmitAnswerRequest> Answers { get; set; } = new();
}

public class BulkSaveAnswersRequest
{
    public Guid AssessmentId { get; set; }
    public Guid CandidateId { get; set; }
    public List<AnswerSaveRequest> Answers { get; set; } = new();
}

public class AnswerSaveRequest
{
    public Guid QuestionId { get; set; }
    public Guid? SelectedOptionId { get; set; }
    public string? DescriptiveAnswer { get; set; }
    public string? CodingAnswer { get; set; }
    public bool AutoSaved { get; set; }
}

public class CandidateAnswersResponse
{
    public Guid CandidateId { get; set; }
    public Guid AssessmentId { get; set; }
    public List<AnswerDto> Answers { get; set; } = new();
    public int TotalScore { get; set; }
    public int? MaxScore { get; set; }
    public DateTime? SubmittedAt { get; set; }
}
