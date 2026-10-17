using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Abstractions.Persistence;
using FiapCloudGames.Identity.Application.Validation;
using FiapCloudGames.Identity.Domain.Users;
using Microsoft.Extensions.Logging;

namespace FiapCloudGames.Identity.Application.Features.Users.Update;

public sealed class UpdateUserService(
    IUserRepository users,
    IIdentityUnitOfWork unitOfWork,
    ILogger<UpdateUserService> logger)
{
    public async Task<UserResult> ExecuteAsync(
        Guid id,
        UpdateUserInput input,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Iniciando atualização do usuário {UserId}.",
            id);

        var user = await users.FindByIdAsync(id, cancellationToken);
        if (user is null)
        {
            logger.LogInformation(
                "Não foi possível atualizar: usuário {UserId} não encontrado.",
                id);
            throw AppException.NotFound("Usuário não encontrado.");
        }
        if (!Email.TryCreate(input.Email, out var email))
        {
            throw AppException.Validation(
                [
                    new AppError(
                        "O e-mail informado é inválido.",
                        InputFieldName.From(nameof(UpdateUserInput.Email)))
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
                        InputFieldName.From(nameof(UpdateUserInput.Name)))
                ]);
        }
        if (await users.EmailExistsAsync(email, id, cancellationToken))
        {
            logger.LogInformation(
                "Não foi possível atualizar o usuário {UserId}: e-mail já cadastrado.",
                id);
            throw AppException.Conflict("Já existe um usuário com este e-mail.");
        }

        user.ChangeDetails(input.Name, email);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Usuário {UserId} atualizado com sucesso.",
            user.Id);
        return IdentityApplicationMappings.ToResult(user);
    }
}
