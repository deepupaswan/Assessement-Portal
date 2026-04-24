namespace CandidateService.Domain.Entities;

public class CandidateAssessment
{
    public Guid Id { get; set; }
    public Guid CandidateId { get; set; }
    public Guid AssessmentId { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    public virtual Candidate? Candidate { get; set; }
}
