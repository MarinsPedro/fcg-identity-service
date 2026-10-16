using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Features.Authentication.Common;
using FiapCloudGames.Identity.Application.Features.Authentication.Refresh;
using FiapCloudGames.Identity.Domain.ServiceClients;
using FiapCloudGames.Identity.UnitTests.Support;
using Microsoft.Extensions.Logging.Abstractions;

namespace FiapCloudGames.Identity.UnitTests.Application.Authentication;

public sealed class RefreshUserTokenServiceTests
{
    [Fact(DisplayName = "Renovar token de usuário - deve rotacionar refresh token e persistir substituto")]
    public async Task ExecuteAsync_WithUsableRefreshToken_RotatesAndPersistsReplacement()
    {
        var user = IdentityTestData.CreateUser();
        var current = IdentityTestData.CreateRefreshToken(user.Id);
        var repository = new FakeRefreshTokenRepository { Token = current };
        var unitOfWork = new SpyIdentityUnitOfWork();
        var issuer = new StubTokenIssuer();
        var service = CreateService(
            new FakeUserRepository { UserById = user },
            repository,
            unitOfWork,
            issuer,
            IdentityTestData.CreateClient());

        var result = await service.ExecuteAsync(
            new RefreshUserTokenInput("current-refresh-token", "web-bff"),
            CancellationToken.None);

        Assert.Equal("user-access-token", result.AccessToken);
        Assert.Equal("generated-refresh-token", result.RefreshToken);
        var replacement = Assert.Single(repository.AddedTokens);
        Assert.Equal(replacement.Id, current.ReplacedByTokenId);
        Assert.Equal(IdentityTestData.NowUtc, current.RevokedAtUtc);
        Assert.Equal(1, issuer.UserTokenCalls);
        Assert.Equal(1, unitOfWork.SaveChangesCount);
    }

    [Fact(DisplayName = "Renovar token de usuário - deve rejeitar refresh token inválido sem persistir")]
    public async Task ExecuteAsync_WithInvalidRefreshToken_ReturnsAuthenticationWithoutPersisting()
    {
        var repository = new FakeRefreshTokenRepository();
        var unitOfWork = new SpyIdentityUnitOfWork();
        var issuer = new StubTokenIssuer();
        var service = CreateService(
            new FakeUserRepository(),
            repository,
            unitOfWork,
            issuer,
            IdentityTestData.CreateClient());

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            service.ExecuteAsync(
                new RefreshUserTokenInput("invalid", "web-bff"),
                CancellationToken.None));

        Assert.Equal(AppErrorCategory.Authentication, exception.Category);
        Assert.Empty(repository.AddedTokens);
        Assert.Equal(0, issuer.UserTokenCalls);
        Assert.Equal(0, unitOfWork.SaveChangesCount);
    }

    [Fact(DisplayName = "Renovar token de usuário - deve rejeitar client sem permissão antes de consultar token")]
    public async Task ExecuteAsync_WhenClientCannotIssueUserTokens_ReturnsForbiddenBeforeReadingToken()
    {
        var repository = new FakeRefreshTokenRepository();
        var service = CreateService(
            new FakeUserRepository(),
            repository,
            new SpyIdentityUnitOfWork(),
            new StubTokenIssuer(),
            IdentityTestData.CreateClient(canIssueUserTokens: false));

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            service.ExecuteAsync(
                new RefreshUserTokenInput("refresh-token", "web-bff"),
                CancellationToken.None));

        Assert.Equal(AppErrorCategory.Forbidden, exception.Category);
        Assert.Null(repository.RequestedHash);
    }

    private static RefreshUserTokenService CreateService(
        FakeUserRepository users,
        FakeRefreshTokenRepository refreshTokensRepository,
        SpyIdentityUnitOfWork unitOfWork,
        StubTokenIssuer issuer,
        ServiceClient client)
    {
        var resolver = new AuthenticatedClientResolver(
            new FakeServiceClientRepository { Client = client },
            NullLogger<AuthenticatedClientResolver>.Instance);
        return new RefreshUserTokenService(
            users,
            refreshTokensRepository,
            unitOfWork,
            new StubRefreshTokenProtector(),
            issuer,
            resolver,
            new FixedTimeProvider(IdentityTestData.NowUtc),
            NullLogger<RefreshUserTokenService>.Instance);
    }
}
