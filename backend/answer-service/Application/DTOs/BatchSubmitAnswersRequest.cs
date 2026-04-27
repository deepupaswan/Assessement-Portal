namespace AnswerService.Application.DTOs;

public class BatchSubmitAnswersRequest
{
    public Guid AssessmentId { get; set; }
    public Guid CandidateId { get; set; }
    public List<SubmitAnswerRequest> Answers { get; set; } = new();
}
