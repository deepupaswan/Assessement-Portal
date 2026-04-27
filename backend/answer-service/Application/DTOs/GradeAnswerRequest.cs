namespace AnswerService.Application.DTOs;

public class GradeAnswerRequest
{
    public Guid AnswerId { get; set; }
    public bool IsCorrect { get; set; }
    public int PointsObtained { get; set; }
    public string? Notes { get; set; }
}
