namespace CandidateService.Api.Models;

public class QuestionDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Type { get; set; } = "MCQ";
    public int MaxScore { get; set; } = 1;
    public int Order { get; set; }
    public List<QuestionOptionDto> Options { get; set; } = new();
}
