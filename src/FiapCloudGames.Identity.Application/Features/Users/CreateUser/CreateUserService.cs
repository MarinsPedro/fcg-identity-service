using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Abstractions;
using FiapCloudGames.Identity.Application.Abstractions.Persistence;
using FiapCloudGames.Identity.Domain.Entities;
using FiapCloudGames.Identity.Domain.Repositories;
using FiapCloudGames.Identity.Domain.ValueObjects;

namespace FiapCloudGames.Identity.Application.Features.Users.CreateUser;

public sealed class CreateUserService(
    IUserRepository users,
    IIdentityUnitOfWork unitOfWork,
    IPasswordHasher hasher,
    TimeProvider clock)
{
    public async Task<UserResult> ExecuteAsync(
        CreateUserInput input,
        CancellationToken cancellationToken)
    {
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

        var password = Password.TryCreate(input.Password, out var validPassword)
            ? validPassword
            : throw AppException.Validation(
                [new AppError(Password.InvalidMessage, "password")]);

        if (await users.EmailExistsAsync(email, null, cancellationToken))
        {
            throw AppException.Conflict("Já existe um usuário com este e-mail.");
        }

        var user = User.Create(input.Name, email, hasher.Hash(password), clock.GetUtcNow());
        await users.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return IdentityApplicationMappings.ToResult(user);
    }
}
