namespace MyFSchool.Application.Identity;

public static class SchoolRoles
{
    public const string Administrator = "Administrator";
    public const string Teacher = "Teacher";
    public const string Parent = "Parent";
    public const string Student = "Student";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Administrator,
        Teacher,
        Parent,
        Student
    };

    public static bool TryFromWire(string? value, out string role)
    {
        role = value switch
        {
            "administrator" => Administrator,
            "teacher" => Teacher,
            "parent" => Parent,
            "student" => Student,
            _ => string.Empty
        };
        return role.Length > 0;
    }

    public static string ToWire(string role) => role switch
    {
        Administrator => "administrator",
        Teacher => "teacher",
        Parent => "parent",
        Student => "student",
        _ => throw new InvalidOperationException("Unknown school role.")
    };

    public static IReadOnlyList<string> ToWire(IEnumerable<string> roles) =>
        roles.Select(ToWire).ToArray();
}

public static class SchoolPolicies
{
    public const string AuthenticatedSession = "AuthenticatedSession";
    public const string Administrator = "Administrator";
    public const string Parent = "Parent";
}
