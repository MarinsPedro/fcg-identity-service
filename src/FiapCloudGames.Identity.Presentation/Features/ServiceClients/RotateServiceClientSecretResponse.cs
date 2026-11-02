namespace FiapCloudGames.Identity.Presentation.Features.ServiceClients;

/// <summary>
/// Representa o resultado da rotação do segredo de um service client.
/// </summary>
/// <param name="ClientId">Identificador do service client.</param>
/// <param name="ClientSecret">Novo segredo, retornado uma única vez.</param>
public sealed record RotateServiceClientSecretResponse(
    string ClientId,
    string ClientSecret);
