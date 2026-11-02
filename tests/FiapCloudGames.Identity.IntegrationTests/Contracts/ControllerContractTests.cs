using FiapCloudGames.BuildingBlocks.Web;
using FiapCloudGames.Identity.IntegrationTests.Support;
using FiapCloudGames.Identity.Presentation.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace FiapCloudGames.Identity.IntegrationTests.Contracts;

public sealed class ControllerContractTests : IClassFixture<IdentityApiFactory>
{
    private readonly IReadOnlyList<ControllerActionDescriptor> actions;

    public ControllerContractTests(IdentityApiFactory factory)
    {
        var presentationAssembly = typeof(IdentityPolicies).Assembly;
        actions = factory.Services
            .GetRequiredService<IActionDescriptorCollectionProvider>()
            .ActionDescriptors.Items
            .OfType<ControllerActionDescriptor>()
            .Where(action => action.ControllerTypeInfo.Assembly == presentationAssembly)
            .ToArray();
    }

    [Fact(DisplayName = "Declarar respostas dos controllers - deve incluir sucesso e erro interno")]
    public void AllControllerActions_DeclareSuccessAndInternalServerError()
    {
        Assert.NotEmpty(actions);

        foreach (var action in actions)
        {
            var statuses = GetResponses(action)
                .Select(response => response.StatusCode)
                .ToArray();

            Assert.Contains(statuses, status => status is >= 200 and < 300);
            Assert.Contains(StatusCodes.Status500InternalServerError, statuses);
        }
    }

    [Fact(DisplayName = "Declarar respostas protegidas - deve incluir autenticação e autorização")]
    public void ProtectedControllerActions_DeclareUnauthorizedAndForbidden()
    {
        var protectedActions = actions.Where(IsProtected).ToArray();
        Assert.NotEmpty(protectedActions);

        foreach (var action in protectedActions)
        {
            var statuses = GetResponses(action)
                .Select(response => response.StatusCode)
                .ToArray();

            Assert.Contains(StatusCodes.Status401Unauthorized, statuses);
            Assert.Contains(StatusCodes.Status403Forbidden, statuses);
        }
    }

    [Fact(DisplayName = "Declarar endpoints com payload - deve incluir validação e mídia não suportada")]
    public void BodyBoundControllerActions_DeclareBadRequestAndUnsupportedMediaType()
    {
        var bodyBoundActions = actions
            .Where(action => action.Parameters.Any(parameter =>
                parameter.BindingInfo?.BindingSource?.Id is "Body" or "Form"))
            .ToArray();
        Assert.NotEmpty(bodyBoundActions);

        foreach (var action in bodyBoundActions)
        {
            var statuses = GetResponses(action)
                .Select(response => response.StatusCode)
                .ToArray();

            Assert.Contains(StatusCodes.Status400BadRequest, statuses);
            Assert.Contains(StatusCodes.Status415UnsupportedMediaType, statuses);
        }
    }

    [Fact(DisplayName = "Declarar respostas de erro - deve usar ApiProblemDetails")]
    public void ErrorResponses_DeclareApiProblemDetails()
    {
        Assert.NotEmpty(actions);

        foreach (var action in actions)
        {
            var errorResponses = GetResponses(action)
                .Where(response => response.StatusCode >= 400)
                .ToArray();
            Assert.NotEmpty(errorResponses);

            foreach (var response in errorResponses)
            {
                Assert.Equal(typeof(ApiProblemDetails), response.Type);
            }
        }
    }

    private static bool IsProtected(ControllerActionDescriptor action) =>
        !action.EndpointMetadata.OfType<IAllowAnonymous>().Any() &&
        action.EndpointMetadata.OfType<IAuthorizeData>().Any();

    private static IEnumerable<ProducesResponseTypeAttribute> GetResponses(
        ControllerActionDescriptor action) =>
        action.EndpointMetadata.OfType<ProducesResponseTypeAttribute>();
}
