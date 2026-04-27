namespace CandidateService.Api.Models;

public class CandidateAssessmentSessionDto
{
    public Guid CandidateAssessmentId { get; set; }
    public Guid CandidateId { get; set; }
    public Guid AssessmentId { get; set; }
    public string AssessmentTitle { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public int RemainingSeconds { get; set; }
    public int AllowedViolations { get; set; }
    public List<CandidateQuestionDto> Questions { get; set; } = new();
}
