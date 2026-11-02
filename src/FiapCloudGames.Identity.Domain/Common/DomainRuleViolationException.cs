namespace FiapCloudGames.Identity.Domain.Common;

public sealed class DomainRuleViolationException(string message)
    : Exception(message);
