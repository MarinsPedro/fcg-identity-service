using System.Net;
using System.Text.Json;
using FiapCloudGames.Identity.IntegrationTests.Support;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;

namespace FiapCloudGames.Identity.IntegrationTests.Contracts;

public sealed class OpenApiContractTests(IdentityApiFactory factory)
    : IClassFixture<IdentityApiFactory>
{
    [Fact(DisplayName = "Consultar Swagger em desenvolvimento - deve documentar os controllers")]
    public async Task Swagger_IsAvailableInDevelopmentAndDocumentsControllers()
    {
        using var client = factory.CreateClient();
        using var uiResponse = await client.GetAsync("/swagger/index.html");
        using var documentResponse = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, uiResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, documentResponse.StatusCode);

        await using var stream = await documentResponse.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        var paths = document.RootElement.GetProperty("paths");

        var apiDescriptions = factory.Services
            .GetRequiredService<IApiDescriptionGroupCollectionProvider>()
            .ApiDescriptionGroups.Items
            .SelectMany(group => group.Items)
            .Where(description =>
                description.ActionDescriptor.RouteValues["controller"] is not null)
            .ToArray();
        Assert.NotEmpty(apiDescriptions);
        foreach (var description in apiDescriptions)
        {
            var relativePath = description.RelativePath!
                .Split('?', StringSplitOptions.RemoveEmptyEntries)[0];
            var path = $"/{relativePath}";
            Assert.True(
                paths.TryGetProperty(path, out var pathDocument),
                $"A rota '{path}' não foi publicada no OpenAPI.");
            Assert.True(
                pathDocument.TryGetProperty(
                    description.HttpMethod!.ToLowerInvariant(),
                    out _),
                $"O método '{description.HttpMethod} {path}' não foi publicado no OpenAPI.");
        }

        Assert.True(paths.TryGetProperty("/v1/auth/login", out var loginPath));
        Assert.Equal(
            "Autentica um usuário por meio do service client solicitante.",
            loginPath.GetProperty("post").GetProperty("summary").GetString());
        Assert.True(paths.TryGetProperty("/oauth2/token", out _));
        Assert.True(paths.TryGetProperty("/v1/users", out _));
        Assert.True(paths.TryGetProperty("/v1/service-clients", out _));
        Assert.True(paths.TryGetProperty(
            "/v1/service-clients/{clientId}/secret/rotate",
            out _));
        Assert.True(paths.TryGetProperty("/v1/audit-events", out _));
        Assert.True(paths.TryGetProperty("/.well-known/jwks.json", out _));
        Assert.False(paths.TryGetProperty("/api/auth/login", out _));
        Assert.False(paths.TryGetProperty("/api/v1/auth/login", out _));
        Assert.False(paths.TryGetProperty("/api/v1/users", out _));
        Assert.False(paths.TryGetProperty("/api/v1/service-clients", out _));
        Assert.False(paths.TryGetProperty("/api/v1/audit-events", out _));
        Assert.False(paths.TryGetProperty("/connect/token", out _));
        Assert.False(paths.TryGetProperty("/internal/v1/auth/login", out _));
        Assert.False(paths.TryGetProperty("/internal/v1/users", out _));
        Assert.False(paths.TryGetProperty("/internal/v1/service-clients", out _));
        Assert.False(paths.TryGetProperty("/internal/v1/audit-events", out _));

        var securitySchemes = document.RootElement
            .GetProperty("components")
            .GetProperty("securitySchemes");
        Assert.True(securitySchemes.TryGetProperty("bearer", out _));
    }
}
