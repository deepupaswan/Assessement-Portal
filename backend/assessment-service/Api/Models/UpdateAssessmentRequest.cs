namespace AssessmentService.Api.Models;

public class UpdateAssessmentRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DurationMinutes { get; set; } = 60;
    public bool RandomizeQuestions { get; set; }
}
