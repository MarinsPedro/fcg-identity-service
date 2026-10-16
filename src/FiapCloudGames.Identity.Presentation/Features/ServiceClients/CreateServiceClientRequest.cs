using System.ComponentModel.DataAnnotations;

namespace FiapCloudGames.Identity.Presentation.Features.ServiceClients;

/// <summary>
/// Representa os dados necessários para criar um service client.
/// </summary>
/// <param name="ClientId">Identificador único do client.</param>
/// <param name="DisplayName">Nome de exibição do client.</param>
/// <param name="CanIssueUserTokens">Indica se o client pode iniciar sessões de usuários.</param>
/// <param name="AllowedAudiences">Audiences para as quais o client pode solicitar tokens.</param>
/// <param name="AllowedScopes">Scopes que o client pode solicitar.</param>
public sealed record CreateServiceClientRequest(
    [Required, StringLength(100)] string ClientId,
    [Required, StringLength(200)] string DisplayName,
    bool CanIssueUserTokens,
    [Required, MinLength(1)] string[] AllowedAudiences,
    [Required, MinLength(1)] string[] AllowedScopes);
