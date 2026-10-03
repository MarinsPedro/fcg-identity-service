using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Domain.Users;

namespace FiapCloudGames.Identity.Application.Features.Users.Get;

public sealed class GetUserService(IUserRepository users)
{
    public async Task<UserResult> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(id, cancellationToken);
        return user is null
            ? throw AppException.NotFound("Usuário não encontrado.")
            : IdentityApplicationMappings.ToResult(user);
    }
}
