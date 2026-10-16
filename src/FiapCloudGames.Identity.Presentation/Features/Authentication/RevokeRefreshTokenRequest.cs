using System.ComponentModel.DataAnnotations;

namespace FiapCloudGames.Identity.Presentation.Features.Authentication;

/// <summary>
/// Representa a solicitação de revogação de uma sessão.
/// </summary>
/// <param name="RefreshToken">Refresh token que será revogado.</param>
public sealed record RevokeRefreshTokenRequest([Required] string RefreshToken);
