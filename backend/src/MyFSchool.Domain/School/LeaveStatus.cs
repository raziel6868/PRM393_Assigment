namespace MyFSchool.Domain.School;

public enum LeaveStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Cancelled = 3
}

public enum LeaveReasonCategory
{
    Health = 0,
    Family = 1,
    Personal = 2,
    Academic = 3,
    Other = 4
}

public static class LeaveStatusExtensions
{
    public static string ToWire(this LeaveStatus status) => status switch
    {
        LeaveStatus.Pending => "pending",
        LeaveStatus.Approved => "approved",
        LeaveStatus.Rejected => "rejected",
        LeaveStatus.Cancelled => "cancelled",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    public static bool TryFromWire(string? value, out LeaveStatus status)
    {
        status = value switch
        {
            "pending" => LeaveStatus.Pending,
            "approved" => LeaveStatus.Approved,
            "rejected" => LeaveStatus.Rejected,
            "cancelled" => LeaveStatus.Cancelled,
            _ => default
        };
        return value is "pending" or "approved" or "rejected" or "cancelled";
    }
}

public static class LeaveReasonCategoryExtensions
{
    public static string ToWire(this LeaveReasonCategory category) => category switch
    {
        LeaveReasonCategory.Health => "health",
        LeaveReasonCategory.Family => "family",
        LeaveReasonCategory.Personal => "personal",
        LeaveReasonCategory.Academic => "academic",
        LeaveReasonCategory.Other => "other",
        _ => throw new ArgumentOutOfRangeException(nameof(category))
    };

    public static bool TryFromWire(string? value, out LeaveReasonCategory category)
    {
        category = value switch
        {
            "health" => LeaveReasonCategory.Health,
            "family" => LeaveReasonCategory.Family,
            "personal" => LeaveReasonCategory.Personal,
            "academic" => LeaveReasonCategory.Academic,
            "other" => LeaveReasonCategory.Other,
            _ => default
        };
        return value is "health" or "family" or "personal" or "academic" or "other";
    }
}