using System.ComponentModel.DataAnnotations;

namespace FiapCloudGames.Identity.Presentation.Features.Authentication;

/// <summary>
/// Representa a solicitação de renovação de uma sessão.
/// </summary>
/// <param name="RefreshToken">Refresh token atual da sessão.</param>
public sealed record RefreshRequest([Required] string RefreshToken);
