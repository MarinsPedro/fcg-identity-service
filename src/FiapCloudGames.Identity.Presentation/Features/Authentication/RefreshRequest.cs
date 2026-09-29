using System.ComponentModel.DataAnnotations;

namespace FiapCloudGames.Identity.Presentation.Features.Authentication;

public sealed record RefreshRequest(
    [Required] string RefreshToken,
    [Required, StringLength(100)] string ClientId,
    [Required] string ClientSecret);
