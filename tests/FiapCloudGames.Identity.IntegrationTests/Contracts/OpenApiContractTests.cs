using System.Net;
using System.Text.Json;
using FiapCloudGames.Identity.IntegrationTests.Support;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;

namespace FiapCloudGames.Identity.IntegrationTests.Contracts;

public sealed class OpenApiContractTests(IdentityApiFactory factory)
    : IClassFixture<IdentityApiFactory>
{
    [Fact(DisplayName = "Consultar Swagger em desenvolvimento - deve documentar todos os controllers")]
    public async Task Swagger_IsAvailableInDevelopmentAndDocumentsAllControllers()
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

        var securitySchemes = document.RootElement
            .GetProperty("components")
            .GetProperty("securitySchemes");
        Assert.True(securitySchemes.TryGetProperty("bearer", out _));
    }

    [Fact(DisplayName = "Documentar erros HTTP - deve usar somente o contrato Problem Details fechado")]
    public async Task ErrorResponses_UseOnlyClosedApiProblemDetailsSchema()
    {
        using var document = await GetOpenApiDocumentAsync();
        var root = document.RootElement;
        var schemas = root
            .GetProperty("components")
            .GetProperty("schemas");

        Assert.True(schemas.TryGetProperty("ApiProblemDetails", out var problem));
        Assert.True(schemas.TryGetProperty("ApiError", out var apiError));
        Assert.False(schemas.TryGetProperty("ProblemDetails", out _));
        Assert.False(schemas.TryGetProperty("ValidationProblemDetails", out _));

        AssertSchemaProperties(
            problem,
            "type",
            "title",
            "status",
            "detail",
            "traceId",
            "correlationId",
            "errors");
        AssertSchemaProperties(apiError, "message", "field");
        AssertRequiredProperties(
            problem,
            "type",
            "title",
            "status",
            "detail",
            "traceId");
        AssertRequiredProperties(apiError, "message");
        AssertClosedSchema(problem);
        AssertClosedSchema(apiError);

        foreach (var path in root.GetProperty("paths").EnumerateObject())
        {
            foreach (var operation in path.Value.EnumerateObject())
            {
                var responses = operation.Value.GetProperty("responses");
                Assert.True(
                    responses.TryGetProperty("500", out _),
                    $"{operation.Name.ToUpperInvariant()} {path.Name} não declara 500.");

                foreach (var response in responses.EnumerateObject()
                             .Where(item =>
                                 int.TryParse(item.Name, out var status) &&
                                 status >= 400))
                {
                    var content = response.Value.GetProperty("content");
                    var mediaType = Assert.Single(content.EnumerateObject());
                    Assert.Equal("application/problem+json", mediaType.Name);
                    Assert.Equal(
                        "#/components/schemas/ApiProblemDetails",
                        mediaType.Value
                            .GetProperty("schema")
                            .GetProperty("$ref")
                            .GetString());
                }
            }
        }
    }

    [Fact(DisplayName = "Documentar consulta de auditoria - deve publicar limites e tamanhos dos filtros")]
    public async Task AuditQuery_DocumentsHttpBindingConstraints()
    {
        using var document = await GetOpenApiDocumentAsync();
        var operation = document.RootElement
            .GetProperty("paths")
            .GetProperty("/v1/audit-events")
            .GetProperty("get");

        AssertAuditQueryConstraints(operation);
    }

    private async Task<JsonDocument> GetOpenApiDocumentAsync()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }

    private static void AssertSchemaProperties(
        JsonElement schema,
        params string[] expectedProperties)
    {
        var actualProperties = schema
            .GetProperty("properties")
            .EnumerateObject()
            .Select(property => property.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            expectedProperties.Order(StringComparer.Ordinal),
            actualProperties);
    }

    private static void AssertRequiredProperties(
        JsonElement schema,
        params string[] expectedProperties)
    {
        var required = schema
            .GetProperty("required")
            .EnumerateArray()
            .Select(item => item.GetString())
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            expectedProperties.Order(StringComparer.Ordinal),
            required);
    }

    private static void AssertClosedSchema(JsonElement schema)
    {
        if (schema.TryGetProperty("additionalProperties", out var additional))
        {
            Assert.Equal(JsonValueKind.False, additional.ValueKind);
        }
    }

    private static void AssertAuditQueryConstraints(JsonElement operation)
    {
        var parameters = operation
            .GetProperty("parameters")
            .EnumerateArray()
            .ToDictionary(
                parameter => parameter.GetProperty("name").GetString()!,
                StringComparer.Ordinal);

        var limitSchema = parameters["limit"].GetProperty("schema");
        Assert.Equal(1, limitSchema.GetProperty("minimum").GetInt32());
        Assert.Equal(200, limitSchema.GetProperty("maximum").GetInt32());
        Assert.Equal(50, limitSchema.GetProperty("default").GetInt32());
        Assert.Equal(
            2048,
            parameters["after"]
                .GetProperty("schema")
                .GetProperty("maxLength")
                .GetInt32());
        Assert.Equal(
            100,
            parameters["eventType"]
                .GetProperty("schema")
                .GetProperty("maxLength")
                .GetInt32());
        Assert.Equal(
            100,
            parameters["actorClientId"]
                .GetProperty("schema")
                .GetProperty("maxLength")
                .GetInt32());
        Assert.Equal(
            50,
            parameters["subjectType"]
                .GetProperty("schema")
                .GetProperty("maxLength")
                .GetInt32());
        Assert.Equal(
            254,
            parameters["subjectId"]
                .GetProperty("schema")
                .GetProperty("maxLength")
                .GetInt32());
    }
}
