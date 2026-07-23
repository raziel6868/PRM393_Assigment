namespace MyFSchool.Infrastructure.Configuration;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public bool Enabled { get; set; }
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "MyFSchool";
    public string Security { get; set; } = "startTls";
}
