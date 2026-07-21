namespace MyFSchool.Infrastructure.Configuration;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string Provider { get; set; } = string.Empty;

    public string LocalRoot { get; set; } = string.Empty;
}
