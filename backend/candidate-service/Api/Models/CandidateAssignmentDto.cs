namespace CandidateService.Api.Models;

public class CandidateAssignmentDto
{
    public Guid CandidateAssessmentId { get; set; }
    public Guid CandidateId { get; set; }
    public string CandidateName { get; set; } = string.Empty;
    public Guid AssessmentId { get; set; }
    public string AssessmentTitle { get; set; } = string.Empty;
    public string Status { get; set; } = "Assigned";
    public DateTime AssignedAtUtc { get; set; }
    public DateTime? ScheduledAtUtc { get; set; }
    public DateTime? StartTimeUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public int RemainingSeconds { get; set; }
}
