namespace FiapCloudGames.Identity.Application.Features.Clients.UpdatePermissions;

public sealed record UpdateServiceClientPermissionsInput(
    IReadOnlyCollection<string> AllowedAudiences,
    IReadOnlyCollection<string> AllowedScopes);
