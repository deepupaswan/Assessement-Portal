using CandidateService.Application.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CandidateService.Api.Events
{
    public class CandidateCreatedConsumer : IConsumer<CandidateCreatedEvent>
    {
        private readonly ILogger<CandidateCreatedConsumer> _logger;
        public CandidateCreatedConsumer(ILogger<CandidateCreatedConsumer> logger)
        {
            _logger = logger;
        }

        public Task Consume(ConsumeContext<CandidateCreatedEvent> context)
        {
            _logger.LogInformation($"Received CandidateCreatedEvent: {context.Message.CandidateId}");
            return Task.CompletedTask;
        }
    }
}
