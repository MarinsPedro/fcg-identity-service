namespace FiapCloudGames.Identity.Migrations.Seeding;

internal sealed class AdminSeedOptions
{
    public const string SectionName = "Admin";

    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
}
