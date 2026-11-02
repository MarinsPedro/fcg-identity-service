using FiapCloudGames.Identity.Application.Abstractions.Messaging;
using FiapCloudGames.BuildingBlocks.Observability;
using FiapCloudGames.Identity.Contracts;
using MassTransit;

namespace FiapCloudGames.Identity.Infrastructure.Messaging;

internal sealed class MassTransitIntegrationEventPublisher(
    IPublishEndpoint publishEndpoint,
    ICorrelationContext correlationContext) : IIntegrationEventPublisher
{
    public Task PublishAsync<T>(
        T integrationEvent,
        CancellationToken cancellationToken)
        where T : class, IIntegrationEvent =>
        publishEndpoint.Publish(
            integrationEvent,
            context =>
            {
                var correlationId = correlationContext.CorrelationId;
                context.MessageId = integrationEvent.EventId;

                if (string.IsNullOrWhiteSpace(correlationId))
                {
                    return;
                }

                context.Headers.Set(
                    CorrelationIdConstants.HeaderName,
                    correlationId);

                if (Guid.TryParse(correlationId, out var nativeCorrelationId))
                {
                    context.CorrelationId = nativeCorrelationId;
                }
            },
            cancellationToken);
}
