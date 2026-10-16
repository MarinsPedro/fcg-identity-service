using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Features.Authentication.Common;
using FiapCloudGames.Identity.Application.Features.Authentication.Login;
using FiapCloudGames.Identity.Domain.ServiceClients;
using FiapCloudGames.Identity.UnitTests.Support;
using Microsoft.Extensions.Logging.Abstractions;

namespace FiapCloudGames.Identity.UnitTests.Application.Authentication;

public sealed class LoginServiceTests
{
    [Fact(DisplayName = "Autenticar usuário - deve emitir access e refresh tokens com credenciais válidas")]
    public async Task ExecuteAsync_WithValidCredentials_IssuesAndPersistsTokens()
    {
        var user = IdentityTestData.CreateUser();
        var users = new FakeUserRepository { UserByEmail = user };
        var refreshTokenRepository = new FakeRefreshTokenRepository();
        var unitOfWork = new SpyIdentityUnitOfWork();
        var hasher = new StubPasswordHasher { VerifyResult = true };
        var refreshTokens = new StubRefreshTokenProtector();
        var issuer = new StubTokenIssuer();
        var service = CreateService(
            users,
            refreshTokenRepository,
            unitOfWork,
            hasher,
            refreshTokens,
            issuer,
            IdentityTestData.CreateClient());

        var result = await service.ExecuteAsync(
            new LoginInput(
                "aluno@fiap.com.br",
                "Password1!",
                "web-bff"),
            CancellationToken.None);

        Assert.Equal("user-access-token", result.AccessToken);
        Assert.Equal("generated-refresh-token", result.RefreshToken);
        Assert.Equal(900, result.ExpiresIn);
        var storedToken = Assert.Single(refreshTokenRepository.AddedTokens);
        Assert.Equal(user.Id, storedToken.UserId);
        Assert.Equal("refresh-token-hash", storedToken.TokenHash);
        Assert.Equal(IdentityTestData.NowUtc.AddDays(7), storedToken.ExpiresAtUtc);
        Assert.Equal(1, issuer.UserTokenCalls);
        Assert.Equal(1, unitOfWork.SaveChangesCount);
    }

    [Theory(DisplayName = "Autenticar usuário - deve rejeitar credenciais inválidas sem persistir tokens")]
    [InlineData("invalid-email", false)]
    [InlineData("aluno@fiap.com.br", false)]
    public async Task ExecuteAsync_WithInvalidCredentials_ReturnsAuthenticationWithoutPersisting(
        string email,
        bool passwordMatches)
    {
        var refreshTokenRepository = new FakeRefreshTokenRepository();
        var unitOfWork = new SpyIdentityUnitOfWork();
        var issuer = new StubTokenIssuer();
        var service = CreateService(
            new FakeUserRepository { UserByEmail = IdentityTestData.CreateUser() },
            refreshTokenRepository,
            unitOfWork,
            new StubPasswordHasher { VerifyResult = passwordMatches },
            new StubRefreshTokenProtector(),
            issuer,
            IdentityTestData.CreateClient());

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            service.ExecuteAsync(
                new LoginInput(email, "invalid", "web-bff"),
                CancellationToken.None));

        Assert.Equal(AppErrorCategory.Authentication, exception.Category);
        Assert.Empty(refreshTokenRepository.AddedTokens);
        Assert.Equal(0, issuer.UserTokenCalls);
        Assert.Equal(0, unitOfWork.SaveChangesCount);
    }

    [Fact(DisplayName = "Autenticar usuário - deve rejeitar usuário inativo sem emitir token")]
    public async Task ExecuteAsync_WhenUserIsInactive_ReturnsForbiddenWithoutIssuingToken()
    {
        var issuer = new StubTokenIssuer();
        var unitOfWork = new SpyIdentityUnitOfWork();
        var service = CreateService(
            new FakeUserRepository { UserByEmail = IdentityTestData.CreateUser(active: false) },
            new FakeRefreshTokenRepository(),
            unitOfWork,
            new StubPasswordHasher { VerifyResult = true },
            new StubRefreshTokenProtector(),
            issuer,
            IdentityTestData.CreateClient());

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            service.ExecuteAsync(
                new LoginInput(
                    "aluno@fiap.com.br",
                    "Password1!",
                    "web-bff"),
                CancellationToken.None));

        Assert.Equal(AppErrorCategory.Forbidden, exception.Category);
        Assert.Equal(0, issuer.UserTokenCalls);
        Assert.Equal(0, unitOfWork.SaveChangesCount);
    }

    private static LoginService CreateService(
        FakeUserRepository users,
        FakeRefreshTokenRepository refreshTokenRepository,
        SpyIdentityUnitOfWork unitOfWork,
        StubPasswordHasher hasher,
        StubRefreshTokenProtector refreshTokens,
        StubTokenIssuer issuer,
        ServiceClient client)
    {
        var clientResolver = new AuthenticatedClientResolver(
            new FakeServiceClientRepository { Client = client },
            NullLogger<AuthenticatedClientResolver>.Instance);
        return new LoginService(
            users,
            refreshTokenRepository,
            unitOfWork,
            hasher,
            refreshTokens,
            issuer,
            clientResolver,
            new FixedTimeProvider(IdentityTestData.NowUtc),
            NullLogger<LoginService>.Instance);
    }
}
