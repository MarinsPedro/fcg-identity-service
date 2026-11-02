using System.Text.Json.Serialization;

namespace FiapCloudGames.Identity.Presentation.Features.OAuth;

/// <summary>
/// Representa uma resposta OAuth de token de serviço.
/// </summary>
/// <param name="AccessToken">JWT emitido para o service client.</param>
/// <param name="TokenType">Tipo do token emitido.</param>
/// <param name="ExpiresIn">Tempo de validade do token, em segundos.</param>
/// <param name="Scope">Scopes concedidos ao token.</param>
public sealed record ServiceTokenResponse(
    [property: JsonPropertyName("access_token")]
    string AccessToken,
    [property: JsonPropertyName("token_type")]
    string TokenType,
    [property: JsonPropertyName("expires_in")]
    long ExpiresIn,
    [property: JsonPropertyName("scope")]
    string Scope);
