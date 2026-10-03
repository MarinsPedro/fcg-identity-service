namespace FiapCloudGames.Identity.Application.Abstractions.Security;

public interface IRefreshTokenProtector
{
    string Generate();
    string Hash(string token);
}
