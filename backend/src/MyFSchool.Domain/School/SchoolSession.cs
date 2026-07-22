namespace MyFSchool.Domain.School;

public enum SchoolSession
{
    Morning = 0,
    Afternoon = 1
}

public static class SchoolSessionExtensions
{
    public static string ToWire(this SchoolSession session) => session switch
    {
        SchoolSession.Morning => "morning",
        SchoolSession.Afternoon => "afternoon",
        _ => throw new ArgumentOutOfRangeException(nameof(session))
    };

    public static bool TryFromWire(string? value, out SchoolSession session)
    {
        session = value switch
        {
            "morning" => SchoolSession.Morning,
            "afternoon" => SchoolSession.Afternoon,
            _ => default
        };
        return value is "morning" or "afternoon";
    }
}
