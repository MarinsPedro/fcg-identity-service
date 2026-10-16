using FiapCloudGames.Identity.Application.Features.Authentication.Common;
using FiapCloudGames.Identity.Application.Features.Authentication.Revoke;
using FiapCloudGames.Identity.UnitTests.Support;
using Microsoft.Extensions.Logging.Abstractions;

namespace FiapCloudGames.Identity.UnitTests.Application.Authentication;

public sealed class RevokeRefreshTokenServiceTests
{
    [Fact(DisplayName = "Revogar refresh token - deve revogar e persistir token pertencente ao client")]
    public async Task ExecuteAsync_WithMatchingToken_RevokesAndPersists()
    {
        var user = IdentityTestData.CreateUser();
        var token = IdentityTestData.CreateRefreshToken(user.Id);
        var unitOfWork = new SpyIdentityUnitOfWork();
        var service = CreateService(
            new FakeRefreshTokenRepository { Token = token },
            unitOfWork);

        await service.ExecuteAsync(
            new RevokeRefreshTokenInput("refresh-token", "web-bff"),
            CancellationToken.None);

        Assert.Equal(IdentityTestData.NowUtc, token.RevokedAtUtc);
        Assert.Equal(1, unitOfWork.SaveChangesCount);
    }

    [Fact(DisplayName = "Revogar refresh token - não deve persistir quando o token não existir")]
    public async Task ExecuteAsync_WhenTokenDoesNotExist_CompletesWithoutPersisting()
    {
        var unitOfWork = new SpyIdentityUnitOfWork();
        var service = CreateService(
            new FakeRefreshTokenRepository(),
            unitOfWork);

        await service.ExecuteAsync(
            new RevokeRefreshTokenInput("missing-token", "web-bff"),
            CancellationToken.None);

        Assert.Equal(0, unitOfWork.SaveChangesCount);
    }

    private static RevokeRefreshTokenService CreateService(
        FakeRefreshTokenRepository refreshTokenRepository,
        SpyIdentityUnitOfWork unitOfWork)
    {
        var resolver = new AuthenticatedClientResolver(
            new FakeServiceClientRepository
            {
                Client = IdentityTestData.CreateClient()
            },
            NullLogger<AuthenticatedClientResolver>.Instance);
        return new RevokeRefreshTokenService(
            refreshTokenRepository,
            unitOfWork,
            new StubRefreshTokenProtector(),
            resolver,
            new FixedTimeProvider(IdentityTestData.NowUtc),
            NullLogger<RevokeRefreshTokenService>.Instance);
    }
}
