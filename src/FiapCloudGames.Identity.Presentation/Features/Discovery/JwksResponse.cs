using FiapCloudGames.Identity.Application.Abstractions.Security;

namespace FiapCloudGames.Identity.Presentation.Features.Discovery;

/// <summary>
/// Representa o conjunto de chaves públicas usadas para validar os JWTs.
/// </summary>
/// <param name="Keys">Chaves públicas disponíveis.</param>
public sealed record JwksResponse(IReadOnlyCollection<JwkResponse> Keys)
{
    /// <summary>Converte o documento interno em contrato HTTP.</summary>
    /// <param name="document">Documento de chaves produzido pela aplicação.</param>
    /// <returns>Contrato público do JWKS.</returns>
    public static JwksResponse From(JwksDocument document) =>
        new(document.Keys.Select(JwkResponse.From).ToArray());
}

/// <summary>
/// Representa uma chave pública no formato JSON Web Key.
/// </summary>
/// <param name="Kty">Tipo criptográfico da chave.</param>
/// <param name="Use">Finalidade da chave.</param>
/// <param name="Alg">Algoritmo de assinatura.</param>
/// <param name="Kid">Identificador da chave.</param>
/// <param name="N">Módulo RSA codificado em Base64URL.</param>
/// <param name="E">Expoente RSA codificado em Base64URL.</param>
public sealed record JwkResponse(
    string Kty,
    string Use,
    string Alg,
    string Kid,
    string N,
    string E)
{
    /// <summary>Converte uma chave interna em contrato HTTP.</summary>
    /// <param name="key">Chave produzida pela aplicação.</param>
    /// <returns>Contrato público da chave.</returns>
    public static JwkResponse From(Jwk key) =>
        new(key.Kty, key.Use, key.Alg, key.Kid, key.N, key.E);
}
