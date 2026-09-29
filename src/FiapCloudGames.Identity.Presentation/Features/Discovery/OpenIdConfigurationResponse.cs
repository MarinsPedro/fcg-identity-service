using System.Text.Json.Serialization;

namespace FiapCloudGames.Identity.Presentation.Features.Discovery;

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
