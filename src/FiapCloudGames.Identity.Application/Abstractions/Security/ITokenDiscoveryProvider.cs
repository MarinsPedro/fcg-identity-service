namespace FiapCloudGames.Identity.Application.Abstractions.Security;

public interface ITokenDiscoveryProvider
{
    string Issuer { get; }
    JwksDocument GetJwks();
}

public sealed record JwksDocument(IReadOnlyCollection<Jwk> Keys);

public sealed record Jwk(
    string Kty,
    string Use,
    string Alg,
    string Kid,
    string N,
    string E);
