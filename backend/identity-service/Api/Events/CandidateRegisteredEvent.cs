namespace IdentityService.Api.Events;

public class CandidateRegisteredEvent
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime RegisteredAtUtc { get; set; }
}
