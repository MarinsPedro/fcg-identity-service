using System.Net;
using System.Text.Json;

namespace FiapCloudGames.Identity.IntegrationTests;

public sealed class OpenApiTests(IdentityApiFactory factory)
    : IClassFixture<IdentityApiFactory>
{
    [Fact]
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

        Assert.True(paths.TryGetProperty("/api/v1/auth/login", out _));
        Assert.True(paths.TryGetProperty("/oauth2/token", out _));
        Assert.True(paths.TryGetProperty("/internal/v1/users", out _));
        Assert.True(paths.TryGetProperty("/internal/v1/service-clients", out _));
        Assert.True(paths.TryGetProperty(
            "/internal/v1/service-clients/{clientId}/secret/rotate",
            out _));
        Assert.True(paths.TryGetProperty("/internal/v1/audit-events", out _));
        Assert.True(paths.TryGetProperty("/.well-known/jwks.json", out _));
        Assert.False(paths.TryGetProperty("/api/auth/login", out _));
        Assert.False(paths.TryGetProperty("/connect/token", out _));
        Assert.False(paths.TryGetProperty("/internal/users", out _));
        Assert.False(paths.TryGetProperty("/internal/clients", out _));

        var securitySchemes = document.RootElement
            .GetProperty("components")
            .GetProperty("securitySchemes");
        Assert.True(securitySchemes.TryGetProperty("bearer", out _));
    }
}
