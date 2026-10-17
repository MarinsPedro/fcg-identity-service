using System.ComponentModel.DataAnnotations;
using FiapCloudGames.Identity.Application.Features.ServiceClients;
using FiapCloudGames.Identity.Presentation.Validation;

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
    [Required]
    [ApiStringLength(ServiceClientInputConstraints.ClientIdMaximumLength)]
    string ClientId,
    [Required]
    [ApiStringLength(ServiceClientInputConstraints.DisplayNameMaximumLength)]
    string DisplayName,
    bool CanIssueUserTokens,
    [Required, MinLength(1)] string[] AllowedAudiences,
    [Required, MinLength(1)] string[] AllowedScopes);
