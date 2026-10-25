namespace FiapCloudGames.Identity.Contracts.Events.V1;

public sealed record UserCreatedEvent(
    Guid EventId,
    Guid UserId,
    string Name,
    string Email,
    DateTimeOffset OccurredAtUtc) : IIntegrationEvent
{
    public const string ExchangeName = "identity.user-created.v1";
}
