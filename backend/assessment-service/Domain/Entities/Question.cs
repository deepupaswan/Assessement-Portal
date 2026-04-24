namespace AssessmentService.Domain.Entities;

public class Question
{
    public Guid Id { get; set; }
    public Guid AssessmentId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Type { get; set; } = "MCQ";
    public int MaxScore { get; set; } = 1;
    public string? CorrectAnswer { get; set; }
    public bool IsRequired { get; set; } = true;
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public virtual Assessment? Assessment { get; set; }
    public virtual ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();
}
