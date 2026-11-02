namespace FiapCloudGames.Identity.Application.Features.ServiceClients.UpdatePermissions;

public sealed record UpdateServiceClientPermissionsInput(
    IReadOnlyCollection<string> AllowedAudiences,
    IReadOnlyCollection<string> AllowedScopes);
