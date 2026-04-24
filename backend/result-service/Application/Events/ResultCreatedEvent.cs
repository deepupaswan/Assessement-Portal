namespace ResultService.Application.Events;

public class ResultCreatedEvent
{
    public Guid ResultId { get; set; }
    public Guid AssessmentId { get; set; }
    public Guid CandidateId { get; set; }
    public int Score { get; set; }
    public DateTime CalculatedAt { get; set; }
}
