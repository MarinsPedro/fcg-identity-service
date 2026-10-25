using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Abstractions.Messaging;
using FiapCloudGames.Identity.Application.Abstractions.Security;
using FiapCloudGames.Identity.Application.Abstractions.Persistence;
using FiapCloudGames.Identity.Application.Validation;
using FiapCloudGames.Identity.Contracts.Events.V1;
using FiapCloudGames.Identity.Domain.Users;
using Microsoft.Extensions.Logging;

namespace FiapCloudGames.Identity.Application.Features.Users.Create;

public sealed class CreateUserService(
    IUserRepository users,
    IIdentityUnitOfWork unitOfWork,
    IIntegrationEventPublisher events,
    IPasswordHasher hasher,
    TimeProvider clock,
    ILogger<CreateUserService> logger)
{
    public async Task<UserResult> ExecuteAsync(
        CreateUserInput input,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Iniciando criação de usuário.");

        if (!Email.TryCreate(input.Email, out var email))
        {
            throw AppException.Validation(
                [
                    new AppError(
                        "O e-mail informado é inválido.",
                        InputFieldName.From(nameof(CreateUserInput.Email)))
                ]);
        }

        if (input.Name?.Trim() is not
            {
                Length: >= UserInputConstraints.NameMinimumLength and
                    <= UserInputConstraints.NameMaximumLength
            })
        {
            throw AppException.Validation(
                [
                    new AppError(
                        $"O nome deve ter entre {UserInputConstraints.NameMinimumLength} e " +
                        $"{UserInputConstraints.NameMaximumLength} caracteres.",
                        InputFieldName.From(nameof(CreateUserInput.Name)))
                ]);
        }

        var password = Password.TryCreate(input.Password, out var validPassword)
            ? validPassword
            : throw AppException.Validation(
                [
                    new AppError(
                        Password.InvalidMessage,
                        InputFieldName.From(nameof(CreateUserInput.Password)))
                ]);

        if (await users.EmailExistsAsync(email, null, cancellationToken))
        {
            logger.LogInformation(
                "Criação de usuário rejeitada: e-mail já cadastrado.");
            throw AppException.Conflict("Já existe um usuário com este e-mail.");
        }

        var user = User.Create(input.Name, email, hasher.Hash(password), clock.GetUtcNow());
        await users.AddAsync(user, cancellationToken);
        await events.PublishAsync(
            new UserCreatedEvent(
                Guid.NewGuid(),
                user.Id,
                user.Name,
                user.Email.Value,
                user.CreatedAtUtc),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Usuário {UserId} criado com sucesso.",
            user.Id);
        return IdentityApplicationMappings.ToResult(user);
    }
}
