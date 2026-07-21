using System.Security.Cryptography;

namespace MyFSchool.Infrastructure.Identity;

internal static class PasswordGenerator
{
    private const string Uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lowercase = "abcdefghijkmnopqrstuvwxyz";
    private const string Digits = "23456789";
    private const string Symbols = "!@#$%*-_+?";
    private static readonly string AllCharacters = Uppercase + Lowercase + Digits + Symbols;

    public static string Generate()
    {
        Span<char> password = stackalloc char[20];
        password[0] = Pick(Uppercase);
        password[1] = Pick(Lowercase);
        password[2] = Pick(Digits);
        password[3] = Pick(Symbols);
        for (var index = 4; index < password.Length; index++)
        {
            password[index] = Pick(AllCharacters);
        }

        RandomNumberGenerator.Shuffle(password);
        return new string(password);
    }

    private static char Pick(string source) => source[RandomNumberGenerator.GetInt32(source.Length)];
}
