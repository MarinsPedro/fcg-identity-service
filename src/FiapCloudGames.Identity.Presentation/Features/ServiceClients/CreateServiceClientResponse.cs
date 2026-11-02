namespace FiapCloudGames.Identity.Presentation.Features.ServiceClients;

/// <summary>
/// Representa o resultado da criação de um service client.
/// </summary>
/// <param name="Client">Dados do client criado.</param>
/// <param name="ClientSecret">Segredo inicial, retornado uma única vez.</param>
public sealed record CreateServiceClientResponse(
    ServiceClientResponse Client,
    string ClientSecret);
