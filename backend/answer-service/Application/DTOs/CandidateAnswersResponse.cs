namespace AnswerService.Application.DTOs;

public class CandidateAnswersResponse
{
    public Guid CandidateId { get; set; }
    public Guid AssessmentId { get; set; }
    public List<AnswerDto> Answers { get; set; } = new();
    public int TotalScore { get; set; }
    public int? MaxScore { get; set; }
    public DateTime? SubmittedAt { get; set; }
}
