using FiapCloudGames.Identity.Application.Abstractions.Auditing;
using FiapCloudGames.Identity.Application.Abstractions.Persistence;
using FiapCloudGames.Identity.Application.Abstractions.Security;
using FiapCloudGames.Identity.Domain.Auditing;
using FiapCloudGames.Identity.Domain.ServiceClients;
using FiapCloudGames.Identity.Domain.Users;

namespace FiapCloudGames.Identity.UnitTests.Support;

internal static class IdentityTestData
{
    internal static readonly DateTimeOffset NowUtc =
        new(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
}

internal sealed class FakeUserRepository : IUserRepository
{
    internal User? UserByEmail { get; set; }
    internal User? UserById { get; set; }
    internal User? AddedUser { get; private set; }
    internal bool EmailExists { get; set; }
    internal int EmailExistsCalls { get; private set; }

    public Task AddAsync(User user, CancellationToken cancellationToken)
    {
        AddedUser = user;
        return Task.CompletedTask;
    }

    public Task<User?> FindByEmailAsync(
        Email email,
        CancellationToken cancellationToken) =>
        Task.FromResult(UserByEmail?.Email == email ? UserByEmail : null);

    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(UserById?.Id == id ? UserById : null);

    public Task<bool> EmailExistsAsync(
        Email email,
        Guid? exceptUserId,
        CancellationToken cancellationToken)
    {
        EmailExistsCalls++;
        return Task.FromResult(EmailExists);
    }
}

internal sealed class FakeServiceClientRepository : IServiceClientRepository
{
    internal ServiceClient? Client { get; set; }
    internal ServiceClient? AddedClient { get; private set; }

    public Task<ServiceClient?> FindByIdAsync(
        string clientId,
        CancellationToken cancellationToken) =>
        Task.FromResult(Client?.ClientId == clientId ? Client : null);

    public Task AddAsync(
        ServiceClient client,
        CancellationToken cancellationToken)
    {
        AddedClient = client;
        return Task.CompletedTask;
    }
}

internal sealed class FakeSecurityAuditRepository : ISecurityAuditRepository
{
    internal List<SecurityAuditEvent> AddedEvents { get; } = [];

    public Task AddAsync(
        SecurityAuditEvent auditEvent,
        CancellationToken cancellationToken)
    {
        AddedEvents.Add(auditEvent);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<SecurityAuditEvent>> GetRecentAsync(
        int take,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<SecurityAuditEvent>>([]);
}

internal sealed class SpyIdentityUnitOfWork : IIdentityUnitOfWork
{
    internal int SaveChangesCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveChangesCount++;
        return Task.FromResult(1);
    }
}

internal sealed class StubPasswordHasher : IPasswordHasher
{
    internal string HashResult { get; set; } = "password-hash";
    internal string SecretHashResult { get; set; } = "secret-hash";
    internal int HashCalls { get; private set; }
    internal int HashSecretCalls { get; private set; }

    public string Hash(Password password)
    {
        HashCalls++;
        return HashResult;
    }

    public string HashSecret(string secret)
    {
        HashSecretCalls++;
        return SecretHashResult;
    }

    public bool Verify(string value, string hash) => false;
}

internal sealed class StubClientSecretGenerator : IClientSecretGenerator
{
    internal string Result { get; set; } = "generated-secret";
    internal int GenerateCalls { get; private set; }

    public string Generate()
    {
        GenerateCalls++;
        return Result;
    }
}

internal sealed class FixedTimeProvider(DateTimeOffset nowUtc) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => nowUtc;
}
