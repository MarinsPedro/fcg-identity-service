using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Features.Users.Create;
using FiapCloudGames.Identity.Contracts.Events.V1;
using FiapCloudGames.Identity.UnitTests.Support;
using Microsoft.Extensions.Logging.Abstractions;

namespace FiapCloudGames.Identity.UnitTests.Application.Users;

public sealed class CreateUserServiceTests
{
    [Fact(DisplayName = "Criar usuário - deve criar e persistir com dados válidos")]
    public async Task ExecuteAsync_WithValidInput_CreatesAndPersistsUser()
    {
        var users = new FakeUserRepository();
        var unitOfWork = new SpyIdentityUnitOfWork();
        var events = new SpyIntegrationEventPublisher();
        var hasher = new StubPasswordHasher();
        var service = new CreateUserService(
            users,
            unitOfWork,
            events,
            hasher,
            new FixedTimeProvider(IdentityTestData.NowUtc),
            NullLogger<CreateUserService>.Instance);

        var result = await service.ExecuteAsync(
            new CreateUserInput(
                "  Aluno FIAP  ",
                "  Aluno@FIAP.COM.BR  ",
                "Password1!"),
            CancellationToken.None);

        Assert.NotNull(users.AddedUser);
        Assert.Equal("Aluno FIAP", users.AddedUser.Name);
        Assert.Equal("aluno@fiap.com.br", users.AddedUser.Email.Value);
        Assert.Equal("password-hash", users.AddedUser.PasswordHash);
        Assert.Equal(IdentityTestData.NowUtc, users.AddedUser.CreatedAtUtc);
        Assert.Equal(1, hasher.HashCalls);
        Assert.Equal(1, unitOfWork.SaveChangesCount);
        Assert.Equal(users.AddedUser.Id, result.Id);
        var integrationEvent = Assert.IsType<UserCreatedEvent>(
            Assert.Single(events.PublishedEvents));
        Assert.NotEqual(Guid.Empty, integrationEvent.EventId);
        Assert.Equal(result.Id, integrationEvent.UserId);
        Assert.Equal("Aluno FIAP", integrationEvent.Name);
        Assert.Equal("aluno@fiap.com.br", integrationEvent.Email);
        Assert.Equal(IdentityTestData.NowUtc, integrationEvent.OccurredAtUtc);
    }

    [Fact(DisplayName = "Criar usuário - deve retornar conflito sem persistir quando o e-mail já existir")]
    public async Task ExecuteAsync_WhenEmailAlreadyExists_ReturnsConflictWithoutPersisting()
    {
        var users = new FakeUserRepository { EmailExists = true };
        var unitOfWork = new SpyIdentityUnitOfWork();
        var events = new SpyIntegrationEventPublisher();
        var hasher = new StubPasswordHasher();
        var service = new CreateUserService(
            users,
            unitOfWork,
            events,
            hasher,
            new FixedTimeProvider(IdentityTestData.NowUtc),
            NullLogger<CreateUserService>.Instance);

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            service.ExecuteAsync(
                new CreateUserInput(
                    "Aluno FIAP",
                    "aluno@fiap.com.br",
                    "Password1!"),
                CancellationToken.None));

        Assert.Equal(AppErrorCategory.Conflict, exception.Category);
        Assert.Null(users.AddedUser);
        Assert.Equal(0, hasher.HashCalls);
        Assert.Equal(0, unitOfWork.SaveChangesCount);
        Assert.Empty(events.PublishedEvents);
    }
}
