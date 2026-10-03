using System.ComponentModel.DataAnnotations;

namespace FiapCloudGames.Identity.Presentation.Features.ServiceClients;

public sealed record CreateServiceClientRequest(
    [Required, StringLength(100)] string ClientId,
    [Required, StringLength(200)] string DisplayName,
    bool CanIssueUserTokens,
    [Required, MinLength(1)] string[] AllowedAudiences,
    [Required, MinLength(1)] string[] AllowedScopes);
