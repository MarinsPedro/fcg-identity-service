using System.Reflection;
using System.Xml.Linq;
using FiapCloudGames.Identity.Domain.Users;
using FiapCloudGames.Identity.Infrastructure.Persistence;
using FiapCloudGames.Identity.Presentation.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace FiapCloudGames.Identity.ArchitectureTests;

public sealed class ArchitectureRulesTests
{
    [Fact(DisplayName = "Referenciar projetos - deve seguir a direção da Clean Architecture")]
    public void ProjectReferences_FollowCleanArchitectureDirection()
    {
        AssertProjectReferences("Domain");
        AssertProjectReferences("Application", "Contracts", "Domain");
        AssertProjectReferences("Contracts");
        AssertProjectReferences("Infrastructure", "Application", "Contracts", "Domain");
        AssertProjectReferences("Presentation", "Application");
        AssertProjectReferences("Api", "Infrastructure", "Presentation");
        AssertProjectReferences("Migrations", "Infrastructure");
    }

    [Fact(DisplayName = "Referenciar testes unitários - deve incluir somente Domain e Application")]
    public void UnitTests_ReferenceOnlyDomainAndApplication()
    {
        var root = FindRepositoryRoot();
        var projectPath = Path.Combine(
            root,
            "tests",
            "FiapCloudGames.Identity.UnitTests",
            "FiapCloudGames.Identity.UnitTests.csproj");
        var project = XDocument.Load(projectPath);
        var projectReferences = project
            .Descendants("ProjectReference")
            .Select(element => Path.GetFileNameWithoutExtension(
                (string?)element.Attribute("Include")))
            .OfType<string>()
            .Order(StringComparer.Ordinal)
            .ToArray();
        var packageReferences = project
            .Descendants("PackageReference")
            .Select(element => (string?)element.Attribute("Include") ?? string.Empty)
            .ToArray();

        Assert.Equal(
            [
                "FiapCloudGames.Identity.Application",
                "FiapCloudGames.Identity.Domain"
            ],
            projectReferences);
        Assert.DoesNotContain(
            packageReferences,
            package => package.Contains(
                "EntityFrameworkCore",
                StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            packageReferences,
            package => package.Contains(
                "Mvc.Testing",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact(DisplayName = "Referenciar o domínio - não deve incluir camadas externas ou frameworks")]
    public void Domain_DoesNotReferenceOuterLayersOrFrameworks()
    {
        AssertDoesNotReference(
            typeof(User).Assembly,
            "Application",
            "Infrastructure",
            "Presentation",
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore");
    }

    [Fact(DisplayName = "Referenciar a aplicação - não deve incluir camadas externas, HTTP ou dados")]
    public void Application_DoesNotReferenceOuterLayersOrHttpAndDataFrameworks()
    {
        AssertDoesNotReference(
            typeof(FiapCloudGames.Identity.Application.DependencyInjection).Assembly,
            "Infrastructure",
            "Presentation",
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore");
    }

    [Fact(DisplayName = "Declarar controllers - deve ocorrer somente na camada Presentation")]
    public void Controllers_LiveOnlyInPresentation()
    {
        var presentationAssembly = typeof(IdentityPolicies).Assembly;
        var productionAssemblies = new[]
        {
            typeof(User).Assembly,
            typeof(FiapCloudGames.Identity.Application.DependencyInjection).Assembly,
            typeof(IdentityDbContext).Assembly,
            presentationAssembly,
            typeof(Program).Assembly
        };
        var controllers = productionAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type =>
                !type.IsAbstract &&
                typeof(ControllerBase).IsAssignableFrom(type))
            .ToArray();

        Assert.NotEmpty(controllers);
        Assert.All(
            controllers,
            controller => Assert.Same(presentationAssembly, controller.Assembly));
    }

    [Fact(DisplayName = "Expor estado do domínio - não deve permitir setters públicos")]
    public void DomainTypes_DoNotExposePublicPropertySetters()
    {
        var violations = GetDomainClasses()
            .SelectMany(type => type.GetProperties()
                .Where(property => property.SetMethod?.IsPublic == true)
                .Select(property => $"{type.FullName}.{property.Name}"))
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact(DisplayName = "Construir tipos do domínio - não deve permitir construtores públicos")]
    public void DomainTypes_DoNotExposePublicConstructors()
    {
        var violations = GetDomainClasses()
            .Where(type => type.GetConstructors(
                BindingFlags.Public | BindingFlags.Instance).Length > 0)
            .Select(type => type.FullName)
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact(DisplayName = "Retornar dados dos controllers - não deve expor tipos internos de Application ou Domain")]
    public void ControllerActions_DoNotExposeInternalLayerTypes()
    {
        var violations = typeof(IdentityPolicies).Assembly
            .GetTypes()
            .Where(type =>
                !type.IsAbstract &&
                typeof(ControllerBase).IsAssignableFrom(type))
            .SelectMany(controller => controller
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(method => method.GetCustomAttributes<HttpMethodAttribute>(true).Any())
                .SelectMany(method => GetNestedTypes(method.ReturnType)
                    .Where(IsInternalLayerType)
                    .Select(type => $"{controller.FullName}.{method.Name}: {type.FullName}")))
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact(DisplayName = "Autorizar actions - deve declarar anonimato ou política explícita")]
    public void ControllerActions_DeclareAnonymousAccessOrExplicitPolicy()
    {
        var violations = typeof(IdentityPolicies).Assembly
            .GetTypes()
            .Where(type =>
                !type.IsAbstract &&
                typeof(ControllerBase).IsAssignableFrom(type))
            .SelectMany(controller => controller
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(method => method.GetCustomAttributes<HttpMethodAttribute>(true).Any())
                .Where(method => !AllowsAnonymous(controller, method))
                .Where(method => !HasExplicitPolicy(controller, method))
                .Select(method => $"{controller.FullName}.{method.Name}"))
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact(DisplayName = "Nomear parâmetros de query - não deve declarar nomes manualmente nas propriedades")]
    public void QueryProperties_DoNotDeclareManualNames()
    {
        var violations = typeof(IdentityPolicies).Assembly
            .GetTypes()
            .SelectMany(type => type.GetProperties())
            .Select(property => new
            {
                Property = property,
                Attribute = property.GetCustomAttribute<FromQueryAttribute>()
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Attribute?.Name))
            .Select(item =>
                $"{item.Property.DeclaringType?.FullName}.{item.Property.Name}")
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact(DisplayName = "Declarar migrations do EF - deve ocorrer somente no executável de migrations")]
    public void EfMigrations_LiveOnlyInMigrationsExecutable()
    {
        var root = FindRepositoryRoot();
        var infrastructureMigrations = Path.Combine(
            root,
            "src",
            "FiapCloudGames.Identity.Infrastructure",
            "Persistence",
            "Migrations");
        var migrationsDirectory = Path.Combine(
            root,
            "src",
            "FiapCloudGames.Identity.Migrations",
            "Migrations");
        var migrationsProjectFile = Path.Combine(
            root,
            "src",
            "FiapCloudGames.Identity.Migrations",
            "FiapCloudGames.Identity.Migrations.csproj");
        var migrationsAppSettings = Path.Combine(
            root,
            "src",
            "FiapCloudGames.Identity.Migrations",
            "appsettings.json");

        Assert.False(
            Directory.Exists(infrastructureMigrations) &&
            Directory.GetFiles(
                infrastructureMigrations,
                "*.cs",
                SearchOption.AllDirectories).Length > 0);
        Assert.True(Directory.Exists(migrationsDirectory));
        Assert.NotEmpty(Directory.GetFiles(
            migrationsDirectory,
            "*.cs",
            SearchOption.TopDirectoryOnly));
        Assert.True(File.Exists(migrationsAppSettings));

        var migrationsProject = XDocument.Load(migrationsProjectFile);
        Assert.Equal(
            "Exe",
            migrationsProject.Descendants("OutputType")
                .Single()
                .Value);
        Assert.Equal(
            "PreserveNewest",
            migrationsProject
                .Descendants("None")
                .Single(element =>
                    element.Attribute("Update")?.Value == "appsettings*.json")
                .Element("CopyToOutputDirectory")
                ?.Value);
    }

    private static void AssertDoesNotReference(Assembly assembly, params string[] forbiddenParts)
    {
        var references = assembly.GetReferencedAssemblies().Select(reference => reference.Name ?? string.Empty);
        var violations = references
            .Where(reference => forbiddenParts.Any(
                part => reference.Contains(part, StringComparison.Ordinal)))
            .ToArray();

        Assert.Empty(violations);
    }

    private static Type[] GetDomainClasses() =>
        typeof(User).Assembly
            .GetTypes()
            .Where(type =>
                type.IsClass &&
                type.IsPublic &&
                !type.IsAbstract &&
                type.Namespace?.StartsWith(
                    "FiapCloudGames.Identity.Domain",
                    StringComparison.Ordinal) == true &&
                !typeof(Exception).IsAssignableFrom(type))
            .ToArray();

    private static IEnumerable<Type> GetNestedTypes(Type type)
    {
        yield return type;
        if (type.IsArray)
        {
            foreach (var nested in GetNestedTypes(type.GetElementType()!))
            {
                yield return nested;
            }
        }

        foreach (var argument in type.GetGenericArguments())
        {
            foreach (var nested in GetNestedTypes(argument))
            {
                yield return nested;
            }
        }
    }

    private static bool IsInternalLayerType(Type type) =>
        type.Namespace?.StartsWith(
            "FiapCloudGames.Identity.Application",
            StringComparison.Ordinal) == true ||
        type.Namespace?.StartsWith(
            "FiapCloudGames.Identity.Domain",
            StringComparison.Ordinal) == true;

    private static bool AllowsAnonymous(Type controller, MethodInfo method) =>
        controller.GetCustomAttributes<AllowAnonymousAttribute>(true).Any() ||
        method.GetCustomAttributes<AllowAnonymousAttribute>(true).Any();

    private static bool HasExplicitPolicy(Type controller, MethodInfo method) =>
        controller.GetCustomAttributes<AuthorizeAttribute>(true)
            .Concat(method.GetCustomAttributes<AuthorizeAttribute>(true))
            .Any(attribute => !string.IsNullOrWhiteSpace(attribute.Policy));

    private static void AssertProjectReferences(string layer, params string[] expectedLayers)
    {
        var root = FindRepositoryRoot();
        var projectPath = Path.Combine(
            root,
            "src",
            $"FiapCloudGames.Identity.{layer}",
            $"FiapCloudGames.Identity.{layer}.csproj");
        var references = XDocument.Load(projectPath)
            .Descendants("ProjectReference")
            .Select(element => (string?)element.Attribute("Include"))
            .Where(include => include?.Contains(
                "FiapCloudGames.Identity.",
                StringComparison.Ordinal) == true)
            .Select(Path.GetFileNameWithoutExtension)
            .Select(name => name?.Split('.').Last())
            .Where(name => name is not null)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expectedLayers.Order(StringComparer.Ordinal), references);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null &&
               !File.Exists(Path.Combine(directory.FullName, "FiapCloudGames.Identity.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("A raiz do repositório não foi encontrada.");
    }
}
