using System.Text.Json.Serialization;

namespace FiapCloudGames.Identity.Presentation.Features.OAuth;

public sealed record ServiceTokenResponse(
    [property: JsonPropertyName("access_token")]
    string AccessToken,
    [property: JsonPropertyName("token_type")]
    string TokenType,
    [property: JsonPropertyName("expires_in")]
    long ExpiresIn,
    [property: JsonPropertyName("scope")]
    string Scope);
