using AnswerService.Application.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace AnswerService.Api.Events
{
    public class AnswerCreatedConsumer : IConsumer<AnswerCreatedEvent>
    {
        private readonly ILogger<AnswerCreatedConsumer> _logger;
        public AnswerCreatedConsumer(ILogger<AnswerCreatedConsumer> logger)
        {
            _logger = logger;
        }

        public Task Consume(ConsumeContext<AnswerCreatedEvent> context)
        {
            _logger.LogInformation($"Received AnswerCreatedEvent: {context.Message.AnswerId}");
            return Task.CompletedTask;
        }
    }
}
