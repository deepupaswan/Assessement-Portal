namespace ResultService.Application.DTOs;

public class CreateResultRequest
{
    public Guid CandidateAssessmentId { get; set; }
    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public decimal ScorePercentage { get; set; }
}
