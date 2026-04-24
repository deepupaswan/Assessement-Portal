using AssessmentService.Application.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace AssessmentService.Api.Events
{
    public class AssessmentCreatedConsumer : IConsumer<AssessmentCreatedEvent>
    {
        private readonly ILogger<AssessmentCreatedConsumer> _logger;
        public AssessmentCreatedConsumer(ILogger<AssessmentCreatedConsumer> logger)
        {
            _logger = logger;
        }

        public Task Consume(ConsumeContext<AssessmentCreatedEvent> context)
        {
            _logger.LogInformation($"Received AssessmentCreatedEvent: {context.Message.AssessmentId}");
            return Task.CompletedTask;
        }
    }
}