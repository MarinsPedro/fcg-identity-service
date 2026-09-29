using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Abstractions;
using FiapCloudGames.Identity.Application.Models;
using FiapCloudGames.Identity.Domain.ValueObjects;

namespace FiapCloudGames.Identity.Application.Services;

public sealed class GetUserService(IIdentityStore store)
{
    public async Task<UserResult> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await store.FindUserByIdAsync(id, cancellationToken);
        return user is null
            ? throw AppException.NotFound("Usuário não encontrado.")
            : IdentityApplicationMappings.ToResult(user);
    }
}

public sealed class UpdateUserService(IIdentityStore store)
{
    public async Task<UserResult> ExecuteAsync(
        Guid id,
        string name,
        string emailValue,
        CancellationToken cancellationToken)
    {
        var user = await store.FindUserByIdAsync(id, cancellationToken)
            ?? throw AppException.NotFound("Usuário não encontrado.");
        if (!Email.TryCreate(emailValue, out var email))
        {
            throw AppException.Validation(
                [new AppError("O e-mail informado é inválido.", "email")]);
        }

        if (name?.Trim() is not { Length: >= 2 and <= 120 })
        {
            throw AppException.Validation(
                [new AppError("O nome deve ter entre 2 e 120 caracteres.", "name")]);
        }
        if (await store.UserEmailExistsAsync(email, id, cancellationToken))
        {
            throw AppException.Conflict("Já existe um usuário com este e-mail.");
        }

        user.ChangeDetails(name, email);
        await store.SaveChangesAsync(cancellationToken);
        return IdentityApplicationMappings.ToResult(user);
    }
}

public sealed class DeactivateUserService(IIdentityStore store)
{
    public async Task ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await store.FindUserByIdAsync(id, cancellationToken)
            ?? throw AppException.NotFound("Usuário não encontrado.");
        if (!user.IsActive)
        {
            throw AppException.Conflict("Usuário já desativado.");
        }

        user.Deactivate();
        await store.SaveChangesAsync(cancellationToken);
    }
}
