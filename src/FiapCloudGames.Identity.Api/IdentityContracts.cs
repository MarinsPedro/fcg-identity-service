using System.ComponentModel.DataAnnotations;

namespace FiapCloudGames.Identity.Api;

public sealed record LoginRequest(
    [Required, EmailAddress, StringLength(254)] string Email,
    [Required] string Password,
    [Required, StringLength(100)] string ClientId,
    [Required] string ClientSecret);

public sealed record RefreshRequest(
    [Required] string RefreshToken,
    [Required, StringLength(100)] string ClientId,
    [Required] string ClientSecret);

public sealed record RevokeRefreshTokenRequest(
    [Required] string RefreshToken,
    [Required, StringLength(100)] string ClientId,
    [Required] string ClientSecret);

public sealed record CreateUserRequest(
    [Required, StringLength(120, MinimumLength = 2)] string Name,
    [Required, EmailAddress, StringLength(254)] string Email,
    [Required, MinLength(8)] string Password);

public sealed record UpdateUserRequest(
    [Required, StringLength(120, MinimumLength = 2)] string Name,
    [Required, EmailAddress, StringLength(254)] string Email);
