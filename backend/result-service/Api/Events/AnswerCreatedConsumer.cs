using AnswerService.Application.Events;
using MassTransit;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ResultService.Application.Services;

namespace ResultService.Api.Events;

public class AnswerCreatedConsumer : IConsumer<AnswerCreatedEvent>
{
    private static readonly TimeSpan ProcessedMessageTtl = TimeSpan.FromMinutes(30);
    private readonly IResultService _resultService;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<AnswerCreatedConsumer> _logger;

    public AnswerCreatedConsumer(
        IResultService resultService,
        IMemoryCache memoryCache,
        ILogger<AnswerCreatedConsumer> logger)
    {
        _resultService = resultService;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AnswerCreatedEvent> context)
    {
        if (context.MessageId is Guid messageId)
        {
            var cacheKey = GetCacheKey(messageId);
            if (_memoryCache.TryGetValue(cacheKey, out _))
            {
                _logger.LogInformation("Skipping duplicate AnswerCreatedEvent message {MessageId}", messageId);
                return;
            }
        }

        var message = context.Message;

        _logger.LogInformation(
            "Processing AnswerCreatedEvent for CandidateId={CandidateId}, AssessmentId={AssessmentId}, AnswerId={AnswerId}",
            message.CandidateId,
            message.AssessmentId,
            message.AnswerId);

        await _resultService.CalculateAndPublishAsync(message.CandidateId, message.AssessmentId);

        if (context.MessageId is Guid processedMessageId)
        {
            _memoryCache.Set(GetCacheKey(processedMessageId), true, ProcessedMessageTtl);
        }
    }

    private static string GetCacheKey(Guid messageId) => $"answer-created:{messageId}";
}
