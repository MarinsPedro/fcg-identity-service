using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Abstractions.Persistence;
using FiapCloudGames.Identity.Domain.Repositories;

namespace FiapCloudGames.Identity.Application.Features.Users.DeactivateUser;

public sealed class DeactivateUserService(
    IUserRepository users,
    IIdentityUnitOfWork unitOfWork)
{
    public async Task ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(id, cancellationToken)
            ?? throw AppException.NotFound("Usuário não encontrado.");
        if (!user.IsActive)
        {
            throw AppException.Conflict("Usuário já desativado.");
        }

        user.Deactivate();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
