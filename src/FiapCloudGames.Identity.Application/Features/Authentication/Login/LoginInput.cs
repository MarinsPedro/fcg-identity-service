namespace FiapCloudGames.Identity.Application.Features.Authentication.Login;

public sealed record LoginInput(
    string Email,
    string Password,
    string ClientId);
