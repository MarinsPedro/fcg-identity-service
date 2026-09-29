namespace FiapCloudGames.Identity.Domain;

public sealed class DomainRuleViolationException(string message)
    : Exception(message);
