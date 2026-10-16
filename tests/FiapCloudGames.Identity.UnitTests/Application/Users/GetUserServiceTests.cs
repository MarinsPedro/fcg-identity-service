using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Features.Users.Get;
using FiapCloudGames.Identity.UnitTests.Support;
using Microsoft.Extensions.Logging.Abstractions;

namespace FiapCloudGames.Identity.UnitTests.Application.Users;

public sealed class GetUserServiceTests
{
    [Fact(DisplayName = "Obter usuário - deve retornar usuário existente")]
    public async Task ExecuteAsync_WhenUserExists_ReturnsUser()
    {
        var user = IdentityTestData.CreateUser();
        var service = CreateService(
            new FakeUserRepository { UserById = user });

        var result = await service.ExecuteAsync(
            user.Id,
            CancellationToken.None);

        Assert.Equal(user.Id, result.Id);
        Assert.Equal(user.Email.Value, result.Email);
    }

    [Fact(DisplayName = "Obter usuário - deve retornar não encontrado para usuário inexistente")]
    public async Task ExecuteAsync_WhenUserDoesNotExist_ReturnsNotFound()
    {
        var exception = await Assert.ThrowsAsync<AppException>(() =>
            CreateService(new FakeUserRepository()).ExecuteAsync(
                Guid.NewGuid(),
                CancellationToken.None));

        Assert.Equal(AppErrorCategory.NotFound, exception.Category);
    }

    private static GetUserService CreateService(FakeUserRepository users) =>
        new(users, NullLogger<GetUserService>.Instance);
}
