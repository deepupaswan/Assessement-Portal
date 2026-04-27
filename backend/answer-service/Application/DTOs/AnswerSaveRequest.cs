namespace AnswerService.Application.DTOs;

public class AnswerSaveRequest
{
    public Guid QuestionId { get; set; }
    public Guid? SelectedOptionId { get; set; }
    public string? DescriptiveAnswer { get; set; }
    public string? CodingAnswer { get; set; }
    public bool AutoSaved { get; set; }
}
