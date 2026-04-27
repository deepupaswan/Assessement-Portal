namespace AnswerService.Application.DTOs;

public class BulkSaveAnswersRequest
{
    public Guid AssessmentId { get; set; }
    public Guid CandidateId { get; set; }
    public List<AnswerSaveRequest> Answers { get; set; } = new();
}
