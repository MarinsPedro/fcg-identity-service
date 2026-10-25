using FiapCloudGames.Identity.Contracts;

namespace FiapCloudGames.Identity.Application.Abstractions.Messaging;

public interface IIntegrationEventPublisher
{
    Task PublishAsync<T>(
        T integrationEvent,
        CancellationToken cancellationToken)
        where T : class, IIntegrationEvent;
}
