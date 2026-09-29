using System.ComponentModel.DataAnnotations;

namespace FiapCloudGames.Identity.Presentation.Features.Authentication;

public sealed record LoginRequest(
    [Required, EmailAddress, StringLength(254)] string Email,
    [Required] string Password,
    [Required, StringLength(100)] string ClientId,
    [Required] string ClientSecret);
