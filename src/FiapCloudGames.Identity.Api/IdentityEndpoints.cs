using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Services;
using FiapCloudGames.Identity.Infrastructure.Authentication;
using Microsoft.Extensions.Options;

namespace FiapCloudGames.Identity.Api;

public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/auth/login", LoginAsync).AllowAnonymous();
        endpoints.MapPost("/api/auth/refresh", RefreshAsync).AllowAnonymous();
        endpoints.MapPost("/api/auth/revoke", RevokeAsync).AllowAnonymous();
        endpoints.MapPost("/connect/token", TokenAsync).AllowAnonymous();
        endpoints.MapGet("/.well-known/jwks.json", (IJwksProvider keys) => keys.GetDocument())
            .AllowAnonymous();
        endpoints.MapGet("/.well-known/openid-configuration", Discovery)
            .AllowAnonymous();

        var users = endpoints.MapGroup("/internal/users");
        users.MapPost("/", CreateUserAsync).RequireAuthorization(IdentityPolicies.UsersCreate);
        users.MapGet("/{id:guid}", GetUserAsync).RequireAuthorization(IdentityPolicies.UsersRead);
        users.MapPut("/{id:guid}", UpdateUserAsync).RequireAuthorization(IdentityPolicies.UsersWrite);
        users.MapDelete("/{id:guid}", DeactivateUserAsync).RequireAuthorization(IdentityPolicies.UsersWrite);
        return endpoints;
    }

    private static IResult Discovery(
        HttpContext context,
        IOptions<JwtOptions> jwtOptions)
    {
        var authority = $"{context.Request.Scheme}://{context.Request.Host}";
        return Results.Ok(new
        {
            issuer = jwtOptions.Value.Issuer,
            token_endpoint = $"{authority}/connect/token",
            jwks_uri = $"{authority}/.well-known/jwks.json",
            grant_types_supported = new[] { "client_credentials" },
            token_endpoint_auth_methods_supported = new[] { "client_secret_post" },
            id_token_signing_alg_values_supported = new[] { "RS256" }
        });
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        LoginService service,
        CancellationToken cancellationToken) =>
        Results.Ok(await service.ExecuteAsync(
            request.Email,
            request.Password,
            request.ClientId,
            request.ClientSecret,
            cancellationToken));

    private static async Task<IResult> RefreshAsync(
        RefreshRequest request,
        RefreshUserTokenService service,
        CancellationToken cancellationToken) =>
        Results.Ok(await service.ExecuteAsync(
            request.RefreshToken,
            request.ClientId,
            request.ClientSecret,
            cancellationToken));

    private static async Task<IResult> RevokeAsync(
        RevokeRefreshTokenRequest request,
        RevokeRefreshTokenService service,
        CancellationToken cancellationToken)
    {
        await service.ExecuteAsync(
            request.RefreshToken,
            request.ClientId,
            request.ClientSecret,
            cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> TokenAsync(
        HttpRequest request,
        ClientCredentialsService service,
        CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType)
        {
            throw AppException.Validation("Use application/x-www-form-urlencoded.");
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var result = await service.ExecuteAsync(
            form["grant_type"].ToString(),
            form["client_id"].ToString(),
            form["client_secret"].ToString(),
            form["audience"].ToString(),
            form["scope"].ToString(),
            cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateUserAsync(
        CreateUserRequest request,
        RegisterUserService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ExecuteAsync(
            request.Name,
            request.Email,
            request.Password,
            cancellationToken);
        return Results.Created($"/internal/users/{result.Id}", result);
    }

    private static async Task<IResult> GetUserAsync(
        Guid id,
        GetUserService service,
        CancellationToken cancellationToken) =>
        Results.Ok(await service.ExecuteAsync(id, cancellationToken));

    private static async Task<IResult> UpdateUserAsync(
        Guid id,
        UpdateUserRequest request,
        UpdateUserService service,
        CancellationToken cancellationToken) =>
        Results.Ok(await service.ExecuteAsync(
            id,
            request.Name,
            request.Email,
            cancellationToken));

    private static async Task<IResult> DeactivateUserAsync(
        Guid id,
        DeactivateUserService service,
        CancellationToken cancellationToken)
    {
        await service.ExecuteAsync(id, cancellationToken);
        return Results.NoContent();
    }
}
