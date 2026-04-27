namespace CandidateService.Api.Models;

public class AssessmentDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int DurationMinutes { get; set; } = 60;
}
