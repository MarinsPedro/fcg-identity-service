using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Abstractions;
using FiapCloudGames.Identity.Application.Abstractions.Persistence;
using FiapCloudGames.Identity.Domain;
using FiapCloudGames.Identity.Domain.Entities;
using FiapCloudGames.Identity.Domain.Repositories;

namespace FiapCloudGames.Identity.Application.Features.Clients.CreateClient;

public sealed class CreateServiceClientService(
    IServiceClientRepository clients,
    ISecurityAuditRepository auditEvents,
    IIdentityUnitOfWork unitOfWork,
    IClientSecretGenerator secretGenerator,
    IPasswordHasher passwordHasher,
    TimeProvider clock)
{
    public async Task<CreateServiceClientResult> ExecuteAsync(
        CreateServiceClientInput input,
        string actorClientId,
        CancellationToken cancellationToken)
    {
        var normalizedClientId = input.ClientId?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedClientId))
        {
            throw AppException.Validation("Informe o client_id.");
        }

        if (await clients.FindByIdAsync(normalizedClientId, cancellationToken) is not null)
        {
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

        return new CreateServiceClientResult(
            ServiceClientResult.From(client),
            rawSecret);
    }
}
