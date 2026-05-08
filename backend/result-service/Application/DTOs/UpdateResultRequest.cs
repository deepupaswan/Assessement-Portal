namespace ResultService.Application.DTOs;

public class UpdateResultRequest
{
    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public decimal ScorePercentage { get; set; }
}
