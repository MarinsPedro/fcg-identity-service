using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Abstractions.Persistence;
using FiapCloudGames.Identity.Domain.Users;

namespace FiapCloudGames.Identity.Application.Features.Users.Update;

public sealed class UpdateUserService(
    IUserRepository users,
    IIdentityUnitOfWork unitOfWork)
{
    public async Task<UserResult> ExecuteAsync(
        Guid id,
        UpdateUserInput input,
        CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(id, cancellationToken)
            ?? throw AppException.NotFound("Usuário não encontrado.");
        if (!Email.TryCreate(input.Email, out var email))
        {
            throw AppException.Validation(
                [new AppError("O e-mail informado é inválido.", "email")]);
        }

        if (input.Name?.Trim() is not { Length: >= 2 and <= 120 })
        {
            throw AppException.Validation(
                [new AppError("O nome deve ter entre 2 e 120 caracteres.", "name")]);
        }
        if (await users.EmailExistsAsync(email, id, cancellationToken))
        {
            throw AppException.Conflict("Já existe um usuário com este e-mail.");
        }

        user.ChangeDetails(input.Name, email);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return IdentityApplicationMappings.ToResult(user);
    }
}
