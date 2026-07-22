using System.Security.Cryptography;
using System.Text;

namespace MyFSchool.Api.Identity;

public static class SessionVersion
{
    public const string ClaimName = "sessionVersion";

    public static string Create(string securityStamp) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(securityStamp)));

    public static bool Matches(string? claimValue, string securityStamp)
    {
        if (string.IsNullOrWhiteSpace(claimValue))
        {
            return false;
        }

        try
        {
            var actual = Convert.FromBase64String(claimValue);
            var expected = SHA256.HashData(Encoding.UTF8.GetBytes(securityStamp));
            return actual.Length == expected.Length && CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
