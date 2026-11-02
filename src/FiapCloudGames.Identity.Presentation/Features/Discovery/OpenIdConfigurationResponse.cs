using System.Text.Json.Serialization;

namespace FiapCloudGames.Identity.Presentation.Features.Discovery;

/// <summary>
/// Representa os metadados de descoberta OpenID Connect do Identity.
/// </summary>
/// <param name="Issuer">Identificador do emissor dos tokens.</param>
/// <param name="TokenEndpoint">Endereço do endpoint de emissão de tokens.</param>
/// <param name="JwksUri">Endereço do documento JWKS.</param>
/// <param name="GrantTypesSupported">Tipos de concessão OAuth suportados.</param>
/// <param name="TokenEndpointAuthMethodsSupported">Métodos de autenticação aceitos no token endpoint.</param>
/// <param name="IdTokenSigningAlgValuesSupported">Algoritmos de assinatura publicados pelo emissor.</param>
public sealed record OpenIdConfigurationResponse(
    [property: JsonPropertyName("issuer")]
    string Issuer,
    [property: JsonPropertyName("token_endpoint")]
    string TokenEndpoint,
    [property: JsonPropertyName("jwks_uri")]
    string JwksUri,
    [property: JsonPropertyName("grant_types_supported")]
    IReadOnlyCollection<string> GrantTypesSupported,
    [property: JsonPropertyName("token_endpoint_auth_methods_supported")]
    IReadOnlyCollection<string> TokenEndpointAuthMethodsSupported,
    [property: JsonPropertyName("id_token_signing_alg_values_supported")]
    IReadOnlyCollection<string> IdTokenSigningAlgValuesSupported);
