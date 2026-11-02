using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Features.Users.Update;
using FiapCloudGames.Identity.UnitTests.Support;
using Microsoft.Extensions.Logging.Abstractions;

namespace FiapCloudGames.Identity.UnitTests.Application.Users;

public sealed class UpdateUserServiceTests
{
    [Fact(DisplayName = "Atualizar usuário - deve alterar dados e persistir entrada válida")]
    public async Task ExecuteAsync_WithValidInput_UpdatesAndPersistsUser()
    {
        var user = IdentityTestData.CreateUser();
        var unitOfWork = new SpyIdentityUnitOfWork();
        var service = CreateService(
            new FakeUserRepository { UserById = user },
            unitOfWork);

        var result = await service.ExecuteAsync(
            user.Id,
            new UpdateUserInput("Novo Nome", "novo@fiap.com.br"),
            CancellationToken.None);

        Assert.Equal("Novo Nome", result.Name);
        Assert.Equal("novo@fiap.com.br", result.Email);
        Assert.Equal(1, unitOfWork.SaveChangesCount);
    }

    [Fact(DisplayName = "Atualizar usuário - deve retornar conflito sem persistir quando e-mail já existir")]
    public async Task ExecuteAsync_WhenEmailAlreadyExists_ReturnsConflictWithoutPersisting()
    {
        var user = IdentityTestData.CreateUser();
        var unitOfWork = new SpyIdentityUnitOfWork();
        var service = CreateService(
            new FakeUserRepository
            {
                UserById = user,
                EmailExists = true
            },
            unitOfWork);

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            service.ExecuteAsync(
                user.Id,
                new UpdateUserInput("Novo Nome", "existente@fiap.com.br"),
                CancellationToken.None));

        Assert.Equal(AppErrorCategory.Conflict, exception.Category);
        Assert.Equal(0, unitOfWork.SaveChangesCount);
    }

    [Fact(DisplayName = "Atualizar usuário - deve retornar não encontrado sem persistir quando usuário não existir")]
    public async Task ExecuteAsync_WhenUserDoesNotExist_ReturnsNotFoundWithoutPersisting()
    {
        var unitOfWork = new SpyIdentityUnitOfWork();
        var service = CreateService(new FakeUserRepository(), unitOfWork);

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            service.ExecuteAsync(
                Guid.NewGuid(),
                new UpdateUserInput("Novo Nome", "novo@fiap.com.br"),
                CancellationToken.None));

        Assert.Equal(AppErrorCategory.NotFound, exception.Category);
        Assert.Equal(0, unitOfWork.SaveChangesCount);
    }

    private static UpdateUserService CreateService(
        FakeUserRepository users,
        SpyIdentityUnitOfWork unitOfWork) =>
        new(users, unitOfWork, NullLogger<UpdateUserService>.Instance);
}
