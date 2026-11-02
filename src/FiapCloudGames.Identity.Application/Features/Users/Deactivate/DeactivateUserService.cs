using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Abstractions.Persistence;
using FiapCloudGames.Identity.Domain.Users;
using Microsoft.Extensions.Logging;

namespace FiapCloudGames.Identity.Application.Features.Users.Deactivate;

public sealed class DeactivateUserService(
    IUserRepository users,
    IIdentityUnitOfWork unitOfWork,
    ILogger<DeactivateUserService> logger)
{
    public async Task ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Iniciando desativação do usuário {UserId}.",
            id);

        var user = await users.FindByIdAsync(id, cancellationToken);
        if (user is null)
        {
            logger.LogInformation(
                "Não foi possível desativar: usuário {UserId} não encontrado.",
                id);
            throw AppException.NotFound("Usuário não encontrado.");
        }

        if (!user.IsActive)
        {
            logger.LogInformation(
                "Não foi possível desativar: usuário {UserId} já está inativo.",
                id);
            throw AppException.Conflict("Usuário já desativado.");
        }

        user.Deactivate();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Usuário {UserId} desativado com sucesso.",
            user.Id);
    }
}
