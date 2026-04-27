namespace CandidateService.Api.Models;

public class AssignmentRequest
{
    public Guid CandidateId { get; set; }
    public Guid AssessmentId { get; set; }
    public DateTime? ScheduledAtUtc { get; set; }
}
