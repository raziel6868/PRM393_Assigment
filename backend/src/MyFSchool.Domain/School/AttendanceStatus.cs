namespace MyFSchool.Domain.School;

public enum AttendanceStatus
{
    Unmarked = 0,
    Present = 1,
    Late = 2,
    ExcusedAbsence = 3,
    UnexcusedAbsence = 4
}

public static class AttendanceStatusExtensions
{
    public static string ToWire(this AttendanceStatus status) => status switch
    {
        AttendanceStatus.Present => "present",
        AttendanceStatus.Late => "late",
        AttendanceStatus.ExcusedAbsence => "excusedAbsence",
        AttendanceStatus.UnexcusedAbsence => "unexcusedAbsence",
        AttendanceStatus.Unmarked => "unmarked",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    public static bool TryFromWire(string? value, out AttendanceStatus status)
    {
        status = value switch
        {
            "present" => AttendanceStatus.Present,
            "late" => AttendanceStatus.Late,
            "excusedAbsence" => AttendanceStatus.ExcusedAbsence,
            "unexcusedAbsence" => AttendanceStatus.UnexcusedAbsence,
            "unmarked" => AttendanceStatus.Unmarked,
            _ => default
        };
        return value is "present" or "late" or "excusedAbsence" or "unexcusedAbsence" or "unmarked";
    }
}
