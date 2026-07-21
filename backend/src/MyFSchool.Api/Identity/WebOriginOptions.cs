namespace MyFSchool.Api.Identity;

public sealed class WebOriginOptions
{
    public const string SectionName = "WebOrigins";

    public string[] AllowedOrigins { get; set; } = [];

    public bool IsTrusted(string? origin) =>
        !string.IsNullOrWhiteSpace(origin) &&
        AllowedOrigins.Contains(origin.TrimEnd('/'), StringComparer.OrdinalIgnoreCase);

    public static bool AreValid(string[] origins) =>
        origins.Length > 0 && origins.All(origin =>
            Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
            string.IsNullOrEmpty(uri.PathAndQuery.Trim('/')) &&
            string.IsNullOrEmpty(uri.Fragment));
}
