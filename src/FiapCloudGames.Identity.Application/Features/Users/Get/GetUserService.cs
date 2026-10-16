using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Domain.Users;
using Microsoft.Extensions.Logging;

namespace FiapCloudGames.Identity.Application.Features.Users.Get;

public sealed class GetUserService(
    IUserRepository users,
    ILogger<GetUserService> logger)
{
    public async Task<UserResult> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        logger.LogDebug("Consultando usuário {UserId}.", id);

        var user = await users.FindByIdAsync(id, cancellationToken);
        if (user is null)
        {
            logger.LogDebug("Usuário {UserId} não encontrado.", id);
            throw AppException.NotFound("Usuário não encontrado.");
        }

        return IdentityApplicationMappings.ToResult(user);
    }
}
