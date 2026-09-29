namespace FiapCloudGames.Identity.Migrations;

internal static class MigrationConnection
{
    private static readonly string[] VariableNames =
    [
        "IDENTITY_MIGRATIONS_CONNECTION",
        "ConnectionStrings__Database",
        "IDENTITY_DESIGN_CONNECTION"
    ];

    public static string GetRequired()
    {
        foreach (var variableName in VariableNames)
        {
            var value = Environment.GetEnvironmentVariable(variableName);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        throw new InvalidOperationException(
            "Configure IDENTITY_MIGRATIONS_CONNECTION ou " +
            "ConnectionStrings__Database para executar as migrations.");
    }
}
