using FiapCloudGames.Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace FiapCloudGames.Identity.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(IdentityDbContext context) : IUserRepository
{
    public Task AddAsync(User user, CancellationToken cancellationToken) =>
        context.Users.AddAsync(user, cancellationToken).AsTask();

    public Task<User?> FindByEmailAsync(Email email, CancellationToken cancellationToken) =>
        context.Users.SingleOrDefaultAsync(user => user.Email == email, cancellationToken);

    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Users.SingleOrDefaultAsync(user => user.Id == id, cancellationToken);

    public Task<bool> EmailExistsAsync(
        Email email,
        Guid? exceptUserId,
        CancellationToken cancellationToken) =>
        context.Users.AnyAsync(
            user => user.Email == email &&
                (!exceptUserId.HasValue || user.Id != exceptUserId.Value),
            cancellationToken);
}
