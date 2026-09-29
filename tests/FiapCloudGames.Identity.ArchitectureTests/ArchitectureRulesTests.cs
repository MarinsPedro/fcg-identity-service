using System.Reflection;
using System.Xml.Linq;
using FiapCloudGames.Identity.Domain.Entities;
using FiapCloudGames.Identity.Presentation;
using Microsoft.AspNetCore.Mvc;

namespace FiapCloudGames.Identity.ArchitectureTests;

public sealed class ArchitectureRulesTests
{
    [Fact]
    public void ProjectReferences_FollowCleanArchitectureDirection()
    {
        AssertProjectReferences("Domain");
        AssertProjectReferences("Application", "Domain");
        AssertProjectReferences("Infrastructure", "Application", "Domain");
        AssertProjectReferences("Presentation", "Application");
        AssertProjectReferences("Api", "Infrastructure", "Presentation");
        AssertProjectReferences("Migrations", "Infrastructure");
    }

    [Fact]
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

    [Fact]
    public void Application_DoesNotReferenceOuterLayersOrHttpAndDataFrameworks()
    {
        AssertDoesNotReference(
            typeof(FiapCloudGames.Identity.Application.DependencyInjection).Assembly,
            "Infrastructure",
            "Presentation",
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore");
    }

    [Fact]
    public void Controllers_LiveOnlyInPresentation()
    {
        var presentationControllers = typeof(IdentityPolicies).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && typeof(ControllerBase).IsAssignableFrom(type))
            .ToArray();
        var apiControllers = typeof(Program).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && typeof(ControllerBase).IsAssignableFrom(type))
            .ToArray();

        Assert.NotEmpty(presentationControllers);
        Assert.Empty(apiControllers);
    }

    [Fact]
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
        Assert.Equal(
            "Exe",
            XDocument.Load(migrationsProjectFile)
                .Descendants("OutputType")
                .Single()
                .Value);
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
            .Select(element => Path.GetFileNameWithoutExtension((string?)element.Attribute("Include")))
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
