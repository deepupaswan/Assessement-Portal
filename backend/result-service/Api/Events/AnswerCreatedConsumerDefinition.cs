using MassTransit;

namespace ResultService.Api.Events;

public class AnswerCreatedConsumerDefinition : ConsumerDefinition<AnswerCreatedConsumer>
{
    public AnswerCreatedConsumerDefinition()
    {
        EndpointName = "result-answer-created";
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<AnswerCreatedConsumer> consumerConfigurator)
    {
        endpointConfigurator.PrefetchCount = 16;
        endpointConfigurator.UseMessageRetry(r =>
            r.Exponential(
                retryLimit: 3,
                minInterval: TimeSpan.FromSeconds(1),
                maxInterval: TimeSpan.FromSeconds(10),
                intervalDelta: TimeSpan.FromSeconds(2)));
        endpointConfigurator.UseInMemoryOutbox();
    }
}
