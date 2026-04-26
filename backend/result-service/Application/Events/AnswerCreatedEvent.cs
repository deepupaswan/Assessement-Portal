namespace AnswerService.Application.Events;

public class AnswerCreatedEvent
{
    public Guid AnswerId { get; set; }
    public Guid AssessmentId { get; set; }
    public Guid CandidateId { get; set; }
    public DateTime SubmittedAt { get; set; }
}
