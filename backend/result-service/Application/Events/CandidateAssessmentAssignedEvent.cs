namespace CandidateService.Application.Events;

public class CandidateAssessmentAssignedEvent
{
    public Guid CandidateAssessmentId { get; set; }
    public Guid CandidateId { get; set; }
    public Guid AssessmentId { get; set; }
    public DateTime AssignedAt { get; set; }
}
