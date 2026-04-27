namespace CandidateService.Api.Models;

public class CandidateQuestionDto
{
    public Guid Id { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public string QuestionType { get; set; } = "MCQ";
    public int Marks { get; set; }
    public List<CandidateQuestionOptionDto> Options { get; set; } = new();
}
