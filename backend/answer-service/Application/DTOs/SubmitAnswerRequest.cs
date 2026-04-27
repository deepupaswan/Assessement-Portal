namespace AnswerService.Application.DTOs;

public class SubmitAnswerRequest
{
    public Guid AssessmentId { get; set; }
    public Guid CandidateId { get; set; }
    public Guid QuestionId { get; set; }
    public Guid? SelectedOptionId { get; set; }
    public string AnswerText { get; set; } = string.Empty;
}
