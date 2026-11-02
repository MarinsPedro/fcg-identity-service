namespace FiapCloudGames.Identity.Infrastructure.Messaging;

internal sealed class RabbitMqOptions
{
    internal const string SectionName = "RabbitMq";

    public string Host { get; init; } = string.Empty;
    public ushort Port { get; init; } = 5672;
    public string VirtualHost { get; init; } = "/";
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
