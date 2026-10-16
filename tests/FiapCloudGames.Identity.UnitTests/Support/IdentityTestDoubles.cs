using FiapCloudGames.Identity.Application.Abstractions.Auditing;
using FiapCloudGames.Identity.Application.Abstractions.Persistence;
using FiapCloudGames.Identity.Application.Abstractions.Security;
using FiapCloudGames.Identity.Domain.Auditing;
using FiapCloudGames.Identity.Domain.ServiceClients;
using FiapCloudGames.Identity.Domain.Sessions;
using FiapCloudGames.Identity.Domain.Users;

namespace FiapCloudGames.Identity.UnitTests.Support;

internal static class IdentityTestData
{
    internal static readonly DateTimeOffset NowUtc =
        new(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);

    internal static User CreateUser(bool active = true)
    {
        var user = User.Create(
            "Aluno FIAP",
            Email.Create("aluno@fiap.com.br"),
            "password-hash",
            NowUtc);
        if (!active)
        {
            user.Deactivate();
        }

        return user;
    }

    internal static ServiceClient CreateClient(
        string clientId = "web-bff",
        bool canIssueUserTokens = true,
        bool active = true,
        IReadOnlyCollection<string>? audiences = null,
        IReadOnlyCollection<string>? scopes = null)
    {
        var client = ServiceClient.Create(
            clientId,
            "Web BFF",
            "secret-hash",
            canIssueUserTokens,
            audiences ?? [clientId, "identity-api"],
            scopes ?? ["store.access"],
            NowUtc);
        if (!active)
        {
            client.Deactivate();
        }

        return client;
    }

    internal static RefreshToken CreateRefreshToken(
        Guid userId,
        string clientId = "web-bff",
        string tokenHash = "refresh-token-hash") =>
        RefreshToken.Create(
            userId,
            clientId,
            tokenHash,
            NowUtc,
            NowUtc.AddHours(1));
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
    internal int FindByIdCalls { get; private set; }

    public Task<ServiceClient?> FindByIdAsync(
        string clientId,
        CancellationToken cancellationToken)
    {
        FindByIdCalls++;
        return Task.FromResult(Client?.ClientId == clientId ? Client : null);
    }

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
    internal IReadOnlyCollection<SecurityAuditEvent> RecentEvents { get; set; } = [];
    internal int? RequestedTake { get; private set; }

    public Task AddAsync(
        SecurityAuditEvent auditEvent,
        CancellationToken cancellationToken)
    {
        AddedEvents.Add(auditEvent);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<SecurityAuditEvent>> GetRecentAsync(
        int take,
        CancellationToken cancellationToken)
    {
        RequestedTake = take;
        return Task.FromResult(RecentEvents);
    }
}

internal sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
{
    internal RefreshToken? Token { get; set; }
    internal List<RefreshToken> AddedTokens { get; } = [];
    internal string? RequestedHash { get; private set; }

    public Task<RefreshToken?> FindByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken)
    {
        RequestedHash = tokenHash;
        return Task.FromResult(Token?.TokenHash == tokenHash ? Token : null);
    }

    public Task AddAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken)
    {
        AddedTokens.Add(refreshToken);
        return Task.CompletedTask;
    }
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
    internal bool VerifyResult { get; set; }
    internal int VerifyCalls { get; private set; }

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

    public bool Verify(string value, string hash)
    {
        VerifyCalls++;
        return VerifyResult;
    }
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

internal sealed class StubRefreshTokenProtector : IRefreshTokenProtector
{
    internal string GenerateResult { get; set; } = "generated-refresh-token";
    internal string HashResult { get; set; } = "refresh-token-hash";
    internal int GenerateCalls { get; private set; }
    internal int HashCalls { get; private set; }

    public string Generate()
    {
        GenerateCalls++;
        return GenerateResult;
    }

    public string Hash(string token)
    {
        HashCalls++;
        return HashResult;
    }
}

internal sealed class StubTokenIssuer : ITokenIssuer
{
    internal AccessTokenResult UserToken { get; set; } =
        new("user-access-token", IdentityTestData.NowUtc.AddMinutes(15));
    internal AccessTokenResult ServiceToken { get; set; } =
        new("service-access-token", IdentityTestData.NowUtc.AddMinutes(5));
    internal TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(7);
    internal int UserTokenCalls { get; private set; }
    internal int ServiceTokenCalls { get; private set; }
    internal IReadOnlyCollection<string>? RequestedScopes { get; private set; }
    internal string? RequestedAudience { get; private set; }

    TimeSpan ITokenIssuer.RefreshTokenLifetime => RefreshTokenLifetime;

    public AccessTokenResult IssueUserToken(
        User user,
        ServiceClient client,
        IReadOnlyCollection<string> scopes)
    {
        UserTokenCalls++;
        RequestedScopes = scopes;
        return UserToken;
    }

    public AccessTokenResult IssueServiceToken(
        ServiceClient client,
        string audience,
        IReadOnlyCollection<string> scopes)
    {
        ServiceTokenCalls++;
        RequestedAudience = audience;
        RequestedScopes = scopes;
        return ServiceToken;
    }
}

internal sealed class FixedTimeProvider(DateTimeOffset nowUtc) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => nowUtc;
}
