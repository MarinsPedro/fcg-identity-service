using System.ComponentModel.DataAnnotations;

namespace FiapCloudGames.Identity.Presentation.Features.ServiceClients;

/// <summary>
/// Representa o conjunto completo de permissões de um service client.
/// </summary>
/// <param name="AllowedAudiences">Audiences permitidas para o client.</param>
/// <param name="AllowedScopes">Scopes permitidos para o client.</param>
public sealed record UpdateServiceClientPermissionsRequest(
    [Required, MinLength(1)] string[] AllowedAudiences,
    [Required, MinLength(1)] string[] AllowedScopes);
