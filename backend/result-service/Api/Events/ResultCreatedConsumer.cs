using MassTransit;
using Microsoft.Extensions.Logging;
using ResultService.Application.Events;

namespace ResultService.Api.Events
{
    public class ResultCreatedConsumer : IConsumer<ResultCreatedEvent>
    {
        private readonly ILogger<ResultCreatedConsumer> _logger;
        public ResultCreatedConsumer(ILogger<ResultCreatedConsumer> logger)
        {
            _logger = logger;
        }

        public Task Consume(ConsumeContext<ResultCreatedEvent> context)
        {
            _logger.LogInformation($"Received ResultCreatedEvent: {context.Message.ResultId}");
            return Task.CompletedTask;
        }
    }
}
