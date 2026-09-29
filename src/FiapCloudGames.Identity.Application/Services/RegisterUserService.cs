using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Abstractions;
using FiapCloudGames.Identity.Application.Models;
using FiapCloudGames.Identity.Domain.Entities;
using FiapCloudGames.Identity.Domain.ValueObjects;

namespace FiapCloudGames.Identity.Application.Services;

public sealed class RegisterUserService(
    IIdentityStore store,
    IPasswordHasher hasher,
    TimeProvider clock)
{
    public async Task<UserResult> ExecuteAsync(
        string name,
        string emailValue,
        string passwordValue,
        CancellationToken cancellationToken)
    {
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

        var password = Password.TryCreate(passwordValue, out var validPassword)
            ? validPassword
            : throw AppException.Validation(
                [new AppError(Password.InvalidMessage, "password")]);

        if (await store.UserEmailExistsAsync(email, null, cancellationToken))
        {
            throw AppException.Conflict("Já existe um usuário com este e-mail.");
        }

        var user = User.Create(name, email, hasher.Hash(password), clock.GetUtcNow());
        await store.AddUserAsync(user, cancellationToken);
        await store.SaveChangesAsync(cancellationToken);
        return IdentityApplicationMappings.ToResult(user);
    }
}
