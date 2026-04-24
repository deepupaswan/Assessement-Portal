namespace AssessmentService.Domain.Entities;

public class Assessment
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public bool IsPublished { get; set; }
    public int? PassingScore { get; set; }
    public bool RandomizeQuestions { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
}
