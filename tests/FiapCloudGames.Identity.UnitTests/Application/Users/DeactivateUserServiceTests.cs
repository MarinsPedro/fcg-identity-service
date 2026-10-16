using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Features.Users.Deactivate;
using FiapCloudGames.Identity.UnitTests.Support;
using Microsoft.Extensions.Logging.Abstractions;

namespace FiapCloudGames.Identity.UnitTests.Application.Users;

public sealed class DeactivateUserServiceTests
{
    [Fact(DisplayName = "Desativar usuário - deve desativar e persistir usuário ativo")]
    public async Task ExecuteAsync_WhenUserIsActive_DeactivatesAndPersists()
    {
        var user = IdentityTestData.CreateUser();
        var unitOfWork = new SpyIdentityUnitOfWork();
        var service = CreateService(
            new FakeUserRepository { UserById = user },
            unitOfWork);

        await service.ExecuteAsync(user.Id, CancellationToken.None);

        Assert.False(user.IsActive);
        Assert.Equal(1, unitOfWork.SaveChangesCount);
    }

    [Fact(DisplayName = "Desativar usuário - deve retornar conflito sem persistir usuário já inativo")]
    public async Task ExecuteAsync_WhenUserIsInactive_ReturnsConflictWithoutPersisting()
    {
        var user = IdentityTestData.CreateUser(active: false);
        var unitOfWork = new SpyIdentityUnitOfWork();
        var service = CreateService(
            new FakeUserRepository { UserById = user },
            unitOfWork);

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            service.ExecuteAsync(user.Id, CancellationToken.None));

        Assert.Equal(AppErrorCategory.Conflict, exception.Category);
        Assert.Equal(0, unitOfWork.SaveChangesCount);
    }

    [Fact(DisplayName = "Desativar usuário - deve retornar não encontrado sem persistir usuário inexistente")]
    public async Task ExecuteAsync_WhenUserDoesNotExist_ReturnsNotFoundWithoutPersisting()
    {
        var unitOfWork = new SpyIdentityUnitOfWork();
        var service = CreateService(new FakeUserRepository(), unitOfWork);

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            service.ExecuteAsync(Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(AppErrorCategory.NotFound, exception.Category);
        Assert.Equal(0, unitOfWork.SaveChangesCount);
    }

    private static DeactivateUserService CreateService(
        FakeUserRepository users,
        SpyIdentityUnitOfWork unitOfWork) =>
        new(users, unitOfWork, NullLogger<DeactivateUserService>.Instance);
}
