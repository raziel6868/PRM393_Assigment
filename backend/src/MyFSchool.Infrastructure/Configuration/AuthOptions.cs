namespace MyFSchool.Infrastructure.Configuration;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string JwtSigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;

    public int RestrictedTokenMinutes { get; set; } = 5;

    public int RefreshTokenDays { get; set; } = 7;

    public int TemporaryPasswordHours { get; set; } = 24;
}
