using FiapCloudGames.Identity.Domain.ServiceClients;

namespace FiapCloudGames.Identity.Application.Features.ServiceClients;

/// <summary>
/// Expõe à borda os limites das entradas de service client definidos pelo domínio.
/// </summary>
public static class ServiceClientInputConstraints
{
    public const int ClientIdMaximumLength =
        ServiceClient.ClientIdMaximumLength;
    public const int DisplayNameMaximumLength =
        ServiceClient.DisplayNameMaximumLength;
}
