namespace CandidateService.Api.Models;

public class CandidateAssignmentDto
{
    public Guid CandidateAssessmentId { get; set; }
    public Guid CandidateId { get; set; }
    public Guid AssessmentId { get; set; }
    public string AssessmentTitle { get; set; } = string.Empty;
    public string Status { get; set; } = "Assigned";
    public DateTime? StartTimeUtc { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public int RemainingSeconds { get; set; }
}
