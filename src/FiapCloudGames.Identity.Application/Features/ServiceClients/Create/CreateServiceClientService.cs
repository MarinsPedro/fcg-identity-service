using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Abstractions.Security;
using FiapCloudGames.Identity.Application.Abstractions.Auditing;
using FiapCloudGames.Identity.Application.Abstractions.Persistence;
using FiapCloudGames.Identity.Domain.Auditing;
using FiapCloudGames.Identity.Domain.Common;
using FiapCloudGames.Identity.Domain.ServiceClients;
using Microsoft.Extensions.Logging;

namespace FiapCloudGames.Identity.Application.Features.ServiceClients.Create;

public sealed class CreateServiceClientService(
    IServiceClientRepository clients,
    ISecurityAuditRepository auditEvents,
    IIdentityUnitOfWork unitOfWork,
    IClientSecretGenerator secretGenerator,
    IPasswordHasher passwordHasher,
    TimeProvider clock,
    ILogger<CreateServiceClientService> logger)
{
    public async Task<CreateServiceClientResult> ExecuteAsync(
        CreateServiceClientInput input,
        string actorClientId,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Iniciando criação de service client pelo client {ActorClientId}.",
            actorClientId);

        var normalizedClientId = input.ClientId?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedClientId))
        {
            throw AppException.Validation("Informe o client_id.");
        }

        if (await clients.FindByIdAsync(normalizedClientId, cancellationToken) is not null)
        {
            logger.LogInformation(
                "Criação rejeitada: service client {ClientId} já existe.",
                normalizedClientId);
            throw AppException.Conflict("Já existe um client com este client_id.");
        }

        var rawSecret = secretGenerator.Generate();
        ServiceClient client;
        try
        {
            client = ServiceClient.Create(
                normalizedClientId,
                input.DisplayName,
                passwordHasher.HashSecret(rawSecret),
                input.CanIssueUserTokens,
                input.AllowedAudiences,
                input.AllowedScopes,
                clock.GetUtcNow());
        }
        catch (DomainRuleViolationException exception)
        {
            throw AppException.Validation(exception.Message);
        }

        await clients.AddAsync(client, cancellationToken);
        await auditEvents.AddAsync(
            SecurityAuditEvent.Create(
                ClientAuditEvents.Created,
                actorClientId,
                "service_client",
                client.ClientId,
                clock.GetUtcNow()),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Service client {ClientId} criado por {ActorClientId}.",
            client.ClientId,
            actorClientId);

        return new CreateServiceClientResult(
            ServiceClientResult.From(client),
            rawSecret);
    }
}
