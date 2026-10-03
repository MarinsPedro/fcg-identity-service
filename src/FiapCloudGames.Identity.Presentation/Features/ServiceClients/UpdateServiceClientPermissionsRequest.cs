using System.ComponentModel.DataAnnotations;

namespace FiapCloudGames.Identity.Presentation.Features.ServiceClients;

public sealed record UpdateServiceClientPermissionsRequest(
    [Required, MinLength(1)] string[] AllowedAudiences,
    [Required, MinLength(1)] string[] AllowedScopes);
