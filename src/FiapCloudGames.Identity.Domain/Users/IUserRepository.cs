namespace FiapCloudGames.Identity.Domain.Users;

public interface IUserRepository
{
    Task AddAsync(User user, CancellationToken cancellationToken);
    Task<User?> FindByEmailAsync(Email email, CancellationToken cancellationToken);
    Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> EmailExistsAsync(
        Email email,
        Guid? exceptUserId,
        CancellationToken cancellationToken);
}
