namespace AssessmentService.Application.DTOs
{
    public class AssessmentDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public bool IsPublished { get; set; }
        public int QuestionCount { get; set; }
        public bool RandomizeQuestions { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
