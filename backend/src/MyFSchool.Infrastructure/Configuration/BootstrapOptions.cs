namespace MyFSchool.Infrastructure.Configuration;

public sealed class BootstrapOptions
{
    public const string SectionName = "Bootstrap";

    public bool Enabled { get; set; }

    public string AdministratorUserName { get; set; } = string.Empty;

    public string AdministratorEmail { get; set; } = string.Empty;

    public string AdministratorDisplayName { get; set; } = string.Empty;

    public string AdministratorPassword { get; set; } = string.Empty;
}
