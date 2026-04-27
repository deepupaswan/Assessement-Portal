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
