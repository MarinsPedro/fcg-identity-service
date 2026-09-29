namespace FiapCloudGames.Identity.Application.Features.Authentication.ClientCredentials;

public sealed record ClientCredentialsInput(
    string GrantType,
    string ClientId,
    string ClientSecret,
    string Audience,
    string Scope);
