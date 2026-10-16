using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FiapCloudGames.Identity.Database.IntegrationTests;

public sealed class EfModelConventionTests
{
    [Fact(DisplayName = "Mapear entidades - deve usar o schema identity e lower_snake_case")]
    public void Entities_ShouldUseIdentitySchemaAndLowerSnakeCase()
    {
        using var context = IdentityDbContextTestFactory.Create();
        var entities = context.Model.GetEntityTypes().ToArray();

        Assert.NotEmpty(entities);

        foreach (var entity in entities)
        {
            var table = Assert.IsType<string>(entity.GetTableName());
            Assert.Equal("identity", entity.GetSchema());
            AssertLowerSnakeCase(table);

            var storeObject = StoreObjectIdentifier.Table(
                table,
                entity.GetSchema());

            foreach (var property in entity.GetProperties())
            {
                var column = Assert.IsType<string>(
                    property.GetColumnName(storeObject));
                AssertLowerSnakeCase(column);
            }
        }
    }

    [Fact(DisplayName = "Mapear entidades - deve definir chaves primárias e pertencer ao domínio Identity")]
    public void Entities_ShouldHavePrimaryKeysAndBelongToIdentityDomain()
    {
        using var context = IdentityDbContextTestFactory.Create();

        foreach (var entity in context.Model.GetEntityTypes())
        {
            Assert.NotNull(entity.FindPrimaryKey());
            Assert.StartsWith(
                "FiapCloudGames.Identity.Domain",
                entity.ClrType.Namespace,
                StringComparison.Ordinal);
        }
    }

    [Fact(DisplayName = "Mapear propriedades decimais - deve definir precisão explicitamente")]
    public void DecimalProperties_ShouldHaveExplicitPrecision()
    {
        using var context = IdentityDbContextTestFactory.Create();
        var decimalProperties = context.Model
            .GetEntityTypes()
            .SelectMany(entity => entity.GetProperties())
            .Where(IsStoredAsDecimal)
            .ToArray();

        foreach (var property in decimalProperties)
        {
            Assert.NotNull(property.GetPrecision());
            Assert.NotNull(property.GetScale());
        }
    }

    private static bool IsStoredAsDecimal(IReadOnlyProperty property)
    {
        var providerType = property.GetValueConverter()?.ProviderClrType ??
                           property.ClrType;
        return providerType == typeof(decimal);
    }

    private static void AssertLowerSnakeCase(string value)
    {
        Assert.True(
            value.Length > 0 &&
            char.IsLower(value[0]) &&
            value.All(character =>
                char.IsLower(character) ||
                char.IsDigit(character) ||
                character == '_'),
            $"'{value}' não segue lower_snake_case.");
    }
}
