using System.Text.Json;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FiapCloudGames.Identity.Api.Configuration;

/// <summary>
/// Publica parâmetros de query em camelCase sem exigir nomes manuais nas Requests.
/// </summary>
internal sealed class CamelCaseQueryParameterOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Parameters is null)
        {
            return;
        }

        foreach (var parameter in operation.Parameters
                     .OfType<OpenApiParameter>()
                     .Where(parameter =>
                         parameter.In == ParameterLocation.Query &&
                         !string.IsNullOrWhiteSpace(parameter.Name)))
        {
            parameter.Name = JsonNamingPolicy.CamelCase.ConvertName(parameter.Name!);
        }
    }
}
