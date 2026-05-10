using CandidateService.Application.Services;
using IdentityService.Api.Events;
using MassTransit;

namespace CandidateService.Api.Events;

public class CandidateRegisteredConsumer : IConsumer<CandidateRegisteredEvent>
{
    private readonly ICandidateService _candidateService;
    private readonly ILogger<CandidateRegisteredConsumer> _logger;

    public CandidateRegisteredConsumer(
        ICandidateService candidateService,
        ILogger<CandidateRegisteredConsumer> logger)
    {
        _candidateService = candidateService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CandidateRegisteredEvent> context)
    {
        var message = context.Message;

        if (string.IsNullOrWhiteSpace(message.Email))
        {
            _logger.LogWarning("CandidateRegisteredEvent ignored due to missing email. UserId: {UserId}", message.UserId);
            return;
        }

        var normalizedEmail = message.Email.Trim().ToLowerInvariant();
        var normalizedName = string.IsNullOrWhiteSpace(message.Name) ? normalizedEmail : message.Name.Trim();

        var existingCandidate = await _candidateService.GetCandidateByEmailAsync(normalizedEmail);
        if (existingCandidate != null)
        {
            // Keep data in sync if candidate already exists.
            if (!string.Equals(existingCandidate.Name, normalizedName, StringComparison.Ordinal))
            {
                await _candidateService.UpdateCandidateAsync(existingCandidate.Id, normalizedName, normalizedEmail);
            }

            _logger.LogInformation("Candidate already exists for {Email}; sync skipped.", normalizedEmail);
            return;
        }

        await _candidateService.CreateCandidateAsync(normalizedName, normalizedEmail);
        _logger.LogInformation("Candidate auto-provisioned from registration event for {Email}", normalizedEmail);
    }
}
