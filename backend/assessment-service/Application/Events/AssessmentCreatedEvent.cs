namespace AssessmentService.Application.Events;

public class AssessmentCreatedEvent
{
    public Guid AssessmentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
