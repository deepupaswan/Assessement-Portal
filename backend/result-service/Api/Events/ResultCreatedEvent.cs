namespace ResultService.Api.Events
{
    public class ResultCreatedEvent
    {
        public Guid ResultId { get; set; }
        public Guid AssessmentId { get; set; }
        public Guid CandidateId { get; set; }
        public double Score { get; set; }
        public DateTime CalculatedAt { get; set; }
    }
}
