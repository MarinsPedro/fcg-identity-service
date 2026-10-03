namespace FiapCloudGames.Identity.Application.Features.Clients;

internal static class ClientAuditEvents
{
    public const string Created = "service_client.created";
    public const string PermissionsChanged = "service_client.permissions_changed";
    public const string SecretRotated = "service_client.secret_rotated";
    public const string Deactivated = "service_client.deactivated";
}
