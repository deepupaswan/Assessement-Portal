namespace CandidateService.Api.Models;

public class AssessmentProgressDto
{
    public Guid CandidateAssessmentId { get; set; }
    public string CandidateName { get; set; } = string.Empty;
    public string Status { get; set; } = "Assigned";
    public int CompletionPercent { get; set; }
    public int SuspiciousEvents { get; set; }
    public int RemainingSeconds { get; set; }
}
