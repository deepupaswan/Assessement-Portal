namespace CandidateService.Api.Models;

public class SuspiciousActivityRequest
{
    public Guid CandidateAssessmentId { get; set; }
    public string ViolationType { get; set; } = string.Empty;
    public string? Metadata { get; set; }
}
